namespace Assertions.Tests;

public class DebugCheckTests
{
    private static int _evaluations;

    private static bool CountedTrue()
    {
        _evaluations++;
        return true;
    }

    private static bool CountedFalse()
    {
        _evaluations++;
        return false;
    }

    [Fact]
    public void Passing_check_never_throws() => DebugCheck.That(1 + 1 == 2);

    [Fact]
    public void Failing_check_matches_build_configuration()
    {
        var x = 1;
        _evaluations = 0;
#if DEBUG
        var ex = Assert.Throws<InvariantViolationException>(() => DebugCheck.That(x > 5));
        Assert.Equal("Debug check failed: x > 5", ex.Message);
        Assert.Equal("x > 5", ex.Expression);
        Assert.Throws<InvariantViolationException>(() => DebugCheck.That(CountedFalse()));
        Assert.Equal(1, _evaluations);
#else
        // Release: both the call and the evaluation of its argument are removed.
        DebugCheck.That(x > 5);
        DebugCheck.That(CountedFalse());
        Assert.Equal(0, _evaluations);
#endif
    }

    [Fact]
    public void Argument_is_evaluated_only_in_debug()
    {
        _evaluations = 0;
        DebugCheck.That(CountedTrue());
#if DEBUG
        Assert.Equal(1, _evaluations);
#else
        Assert.Equal(0, _evaluations);
#endif
    }

    [Fact]
    public void Custom_message_in_debug()
    {
#if DEBUG
        var ex = Assert.Throws<InvariantViolationException>(() => DebugCheck.That(false, "custom"));
        Assert.Equal("custom", ex.Message);
#else
        DebugCheck.That(false, "custom");
#endif
    }
}
