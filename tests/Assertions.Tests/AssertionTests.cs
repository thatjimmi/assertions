using System.Runtime.CompilerServices;

namespace Assertions.Tests;

public class EnsureTests
{
    [Fact]
    public void That_true_does_not_throw() => Ensure.That(1 == 1);

    [Fact]
    public void That_false_reports_expression()
    {
        var total = -1;
        var ex = Assert.Throws<PostconditionViolationException>(() => Ensure.That(total >= 0));
        Assert.Equal("Postcondition failed: total >= 0", ex.Message);
        Assert.Equal("total >= 0", ex.Expression);
    }

    [Fact]
    public void That_custom_message_keeps_expression_property()
    {
        var ex = Assert.Throws<PostconditionViolationException>(() => Ensure.That(false, "boom"));
        Assert.Equal("boom", ex.Message);
        Assert.Equal("false", ex.Expression);
    }

    [Fact]
    public void NotNull_returns_same_instance()
    {
        var o = new object();
        Assert.Same(o, Ensure.NotNull(o));
    }

    [Fact]
    public void NotNull_null_throws_with_message_and_expression()
    {
        string? result = null;
        var ex = Assert.Throws<PostconditionViolationException>(() => Ensure.NotNull(result));
        Assert.Equal("Postcondition failed: result was null", ex.Message);
        Assert.Equal("result", ex.Expression);
    }
}

public class EnsureEqualTests
{
    [Fact]
    public void Equal_matching_values_does_not_throw() => Ensure.Equal(1, 1);

    [Fact]
    public void Equal_mismatch_reports_both_values()
    {
        var actual = 150.25m;
        var expected = 150.00m;
        var ex = Assert.Throws<PostconditionViolationException>(() => Ensure.Equal(actual, expected));
        Assert.Contains($"left: {actual}", ex.Message, StringComparison.Ordinal);
        Assert.Contains($"right: {expected}", ex.Message, StringComparison.Ordinal);
        Assert.Equal("actual == expected", ex.Expression);
    }

    [Fact]
    public void Equal_custom_message()
    {
        var ex = Assert.Throws<PostconditionViolationException>(() => Ensure.Equal(1, 2, "boom"));
        Assert.Equal("boom", ex.Message);
    }
}

public class InvariantTests
{
    [Fact]
    public void Check_true_does_not_throw() => Invariant.Check(true);

    [Fact]
    public void Check_false_reports_expression()
    {
        var balance = -5;
        var ex = Assert.Throws<InvariantViolationException>(() => Invariant.Check(balance >= 0));
        Assert.Equal("Invariant failed: balance >= 0", ex.Message);
        Assert.Equal("balance >= 0", ex.Expression);
    }

    [Fact]
    public void Check_custom_message()
    {
        var ex = Assert.Throws<InvariantViolationException>(() => Invariant.Check(false, "custom"));
        Assert.Equal("custom", ex.Message);
        Assert.Equal("false", ex.Expression);
    }

    [Fact]
    public void Unreachable_reports_member_file_and_line()
    {
        var expectedLine = LineNumber() + 1;
        var ex = Invariant.Unreachable();

        Assert.Equal(
            $"Unreachable code reached in {nameof(Unreachable_reports_member_file_and_line)} (AssertionTests.cs:{expectedLine})",
            ex.Message);
        Assert.Null(ex.Expression);
    }

    [Fact]
    public void Unreachable_appends_message()
    {
        var ex = Invariant.Unreachable("Unhandled status 7");
        Assert.EndsWith(": Unhandled status 7", ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Unreachable_appends_message), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unreachable_is_usable_in_a_throw_expression()
    {
        var ex = Assert.Throws<InvariantViolationException>(() => Label((Status)9));
        Assert.Contains("Unhandled status 9", ex.Message, StringComparison.Ordinal);
    }

    private enum Status { Pending, Shipped }

    private static string Label(Status s) => s switch
    {
        Status.Pending => "Pending",
        Status.Shipped => "Shipped",
        _ => throw Invariant.Unreachable($"Unhandled status {(int)s}"),
    };

    private static int LineNumber([CallerLineNumber] int line = 0) => line;
}

public class InvariantEqualTests
{
    [Fact]
    public void Equal_matching_values_does_not_throw() => Invariant.Equal(42, 42);

    [Fact]
    public void Equal_mismatch_reports_both_values()
    {
        var position = 150.25m;
        var expected = 150.00m;
        var ex = Assert.Throws<InvariantViolationException>(() => Invariant.Equal(position, expected));
        Assert.Contains($"left: {position}", ex.Message, StringComparison.Ordinal);
        Assert.Contains($"right: {expected}", ex.Message, StringComparison.Ordinal);
        Assert.Equal("position == expected", ex.Expression);
    }

    [Fact]
    public void Equal_custom_message()
    {
        var ex = Assert.Throws<InvariantViolationException>(() => Invariant.Equal(1, 2, "mismatch"));
        Assert.Equal("mismatch", ex.Message);
    }
}

public class InvariantCloseTests
{
    [Fact]
    public void Close_within_tolerance_does_not_throw() => Invariant.Close(1.0, 1.0001, 0.001);

    [Fact]
    public void Close_exceeding_tolerance_reports_values()
    {
        var forecast = 150.5;
        var actual = 149.0;
        var ex = Assert.Throws<InvariantViolationException>(() => Invariant.Close(forecast, actual, 1.0));
        Assert.Contains($"left: {forecast}", ex.Message, StringComparison.Ordinal);
        Assert.Contains($"right: {actual}", ex.Message, StringComparison.Ordinal);
        Assert.Contains("diff:", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Close_exact_tolerance_boundary_does_not_throw() => Invariant.Close(1.0, 2.0, 1.0);

    [Fact]
    public void Close_just_over_boundary_throws() =>
        Assert.Throws<InvariantViolationException>(() => Invariant.Close(1.0, 2.001, 1.0));
}
