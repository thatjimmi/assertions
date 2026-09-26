using Assertions;

namespace Assertions.Tests;

// Sample domain, written in the style the README recommends.
public sealed class Account
{
    public Account(decimal opening)
    {
        Balance = Guard.NotNegative(opening);
    }

    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        Guard.Positive(amount);

        Balance += amount;

        Invariant.Check(Balance >= 0);
    }

    public void Withdraw(decimal amount)
    {
        Guard.Positive(amount);
        Guard.Requires(amount <= Balance);

        Balance -= amount;

        Invariant.Check(Balance >= 0);
    }
}

public static class Bank
{
    public static bool TryTransfer(Account from, Account to, decimal amount)
    {
        Guard.NotNull(from);
        Guard.NotNull(to);
        Guard.Positive(amount);
        Guard.Requires(!ReferenceEquals(from, to), "Cannot transfer to the same account.");

        if (amount > from.Balance)
        {
            return false; // Expected outcome, not a bug.
        }

        var totalBefore = from.Balance + to.Balance;

        from.Withdraw(amount);
        to.Deposit(amount);

        Invariant.Check(from.Balance + to.Balance == totalBefore);
        return true;
    }
}
