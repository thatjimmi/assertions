// README code examples, compile-only. Snippet bodies are copied verbatim; only the
// surrounding types are minimal stubs. Never executed.
using Assertions;

namespace ReadmeSamples;

public enum OrderStatus { Pending, Shipped }

public sealed class OrderLine { }

public sealed class Order
{
    public Guid Id { get; }
    public string CustomerName { get; }
    public List<OrderLine> Lines { get; }
    public decimal Total { get; private set; }

    // "Guard" section
    public Order(Guid id, string customerName, List<OrderLine> lines)
    {
        Id = Guard.NotDefault(id);
        CustomerName = Guard.NotNullOrWhiteSpace(customerName);
        Lines = Guard.NotEmpty(lines);
    }

    // "Ensure" section
    public decimal ApplyDiscount(decimal percent)
    {
        Guard.InRange(percent, 0m, 100m);
        var before = Total;

        Total -= Total * percent / 100m;

        Ensure.That(Total >= 0 && Total <= before);
        return Total;
    }

    // "Invariant" section
    public static string Label(OrderStatus status) => Guard.Defined(status) switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.Shipped => "Shipped",
        _ => throw Invariant.Unreachable($"Unhandled status {status}"),
    };

    // "Ensure" section: return Ensure.NotNull(result)
    public static string NeverNull(string? result) => Ensure.NotNull(result);
}

public sealed class Account
{
    public decimal Balance { get; private set; }

    public void Deposit(decimal amount) => Balance += Guard.Positive(amount);

    // Top-of-README and "Prevent, then verify" versions are identical apart from comments.
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

public static class Loops
{
    // "DebugCheck" section
    public static void Sorted(int[] items)
    {
        DebugCheck.That(IsSorted(items));
    }

    private static bool IsSorted(int[] items) => items.Zip(items.Skip(1), (a, b) => a <= b).All(x => x);
}

// Background-job snippet (uses Microsoft.Extensions.Logging in the README, so only the catch shape is checked here).
public static class Jobs
{
    public static async Task RunAsync(Func<Task> process, Func<Task> markFailed)
    {
        try
        {
            await process();
        }
        catch (AssertionFailedException ex) when (ex.Expression is not null)
        {
            await markFailed();
        }
    }
}
