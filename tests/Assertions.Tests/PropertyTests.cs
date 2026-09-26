using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Assertions.Tests;

public class GuardProperties
{
    private static bool Throws<T>(Action a) where T : Exception
    {
        try
        {
            a();
            return false;
        }
        catch (T)
        {
            return true;
        }
    }

    [Property]
    public bool InRange_accepts_exactly_min_le_value_le_max(int value, int min, int max)
    {
        var expected = min <= value && value <= max;
        return expected
            ? Guard.InRange(value, min, max) == value
            : Throws<ArgumentOutOfRangeException>(() => Guard.InRange(value, min, max));
    }

    [Property]
    public bool InRange_accepts_both_bounds(int a, int b)
    {
        var (min, max) = a <= b ? (a, b) : (b, a);
        return Guard.InRange(min, min, max) == min && Guard.InRange(max, min, max) == max;
    }

    [Property]
    public bool Positive_int_accepts_exactly_positive(int n) =>
        n > 0 ? Guard.Positive(n) == n : Throws<ArgumentOutOfRangeException>(() => Guard.Positive(n));

    [Property]
    public bool Positive_decimal_accepts_exactly_positive(decimal n) =>
        n > 0 ? Guard.Positive(n) == n : Throws<ArgumentOutOfRangeException>(() => Guard.Positive(n));

    [Property]
    public bool NotNegative_accepts_exactly_non_negative(long n) =>
        n >= 0 ? Guard.NotNegative(n) == n : Throws<ArgumentOutOfRangeException>(() => Guard.NotNegative(n));

    [Property]
    public bool Positive_double_accepts_exactly_positive(NormalFloat f)
    {
        var n = f.Item; // NaN and infinities excluded.
        return n > 0 ? Guard.Positive(n) == n : Throws<ArgumentOutOfRangeException>(() => Guard.Positive(n));
    }
}

public class BankProperties
{
    [Property]
    public void Random_transfers_conserve_money(NonNegativeInt openingA, NonNegativeInt openingB, PositiveInt[] amounts)
    {
        var a = new Account(openingA.Get);
        var b = new Account(openingB.Get);
        var total = a.Balance + b.Balance;

        for (var i = 0; i < amounts.Length; i++)
        {
            var (from, to) = i % 2 == 0 ? (a, b) : (b, a);
            _ = Bank.TryTransfer(from, to, amounts[i].Get); // Invariants inside check every step.
        }

        Assert.Equal(total, a.Balance + b.Balance);
    }

    [Property]
    public void Balances_never_go_negative(NonNegativeInt openingA, NonNegativeInt openingB, PositiveInt[] amounts)
    {
        var a = new Account(openingA.Get);
        var b = new Account(openingB.Get);

        for (var i = 0; i < amounts.Length; i++)
        {
            var (from, to) = i % 3 == 0 ? (b, a) : (a, b);
            _ = Bank.TryTransfer(from, to, amounts[i].Get);
            Assert.True(a.Balance >= 0 && b.Balance >= 0);
        }
    }

    [Property]
    public void TryTransfer_returns_false_and_changes_nothing_when_funds_are_insufficient(NonNegativeInt opening, PositiveInt extra)
    {
        var a = new Account(opening.Get);
        var b = new Account(0);
        var amount = (decimal)opening.Get + extra.Get;

        Assert.False(Bank.TryTransfer(a, b, amount));
        Assert.Equal(opening.Get, a.Balance);
        Assert.Equal(0, b.Balance);
    }

    [Fact]
    public void Withdraw_rejects_non_positive_amounts()
    {
        var account = new Account(100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Withdraw(0m));
    }

    [Fact]
    public void Withdraw_more_than_balance_is_a_guard_failure_not_an_assertion()
    {
        var account = new Account(10m);
        var ex = Assert.Throws<ArgumentException>(() => account.Withdraw(11m));
        Assert.Equal("Precondition failed: amount <= Balance", ex.Message);
        Assert.Equal(10m, account.Balance);
    }

    [Fact]
    public void TryTransfer_to_same_account_is_rejected()
    {
        var a = new Account(10m);
        var ex = Assert.Throws<ArgumentException>(() => Bank.TryTransfer(a, a, 1m));
        Assert.Equal("Cannot transfer to the same account.", ex.Message);
    }
}
