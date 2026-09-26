namespace Assertions.Tests;

public class ExceptionTests
{
    [Fact]
    public void Hierarchy()
    {
        Assert.True(typeof(AssertionFailedException).IsAbstract);
        Assert.Equal(typeof(Exception), typeof(AssertionFailedException).BaseType);
        Assert.Equal(typeof(AssertionFailedException), typeof(InvariantViolationException).BaseType);
        Assert.Equal(typeof(AssertionFailedException), typeof(PostconditionViolationException).BaseType);
        Assert.True(typeof(InvariantViolationException).IsSealed);
        Assert.True(typeof(PostconditionViolationException).IsSealed);
    }

    [Fact]
    public void Guard_exceptions_are_ArgumentExceptions_not_assertion_failures()
    {
        Assert.False(typeof(AssertionFailedException).IsAssignableFrom(typeof(ArgumentException)));
        Assert.False(typeof(ArgumentException).IsAssignableFrom(typeof(AssertionFailedException)));
    }

    [Fact]
    public void Invariant_standard_constructors()
    {
        var inner = new InvalidOperationException();

        Assert.Equal("An assertion failed.", new InvariantViolationException().Message);
        Assert.Equal("m", new InvariantViolationException("m").Message);
        Assert.Null(new InvariantViolationException("m").Expression);

        var withExpr = new InvariantViolationException("m", "x > 0");
        Assert.Equal("m", withExpr.Message);
        Assert.Equal("x > 0", withExpr.Expression);

        var withInner = new InvariantViolationException("m", inner);
        Assert.Equal("m", withInner.Message);
        Assert.Same(inner, withInner.InnerException);
        Assert.Null(withInner.Expression);
    }

    [Fact]
    public void Postcondition_standard_constructors()
    {
        var inner = new InvalidOperationException();

        Assert.Equal("An assertion failed.", new PostconditionViolationException().Message);
        Assert.Equal("m", new PostconditionViolationException("m").Message);
        Assert.Null(new PostconditionViolationException("m").Expression);

        var withExpr = new PostconditionViolationException("m", "x > 0");
        Assert.Equal("m", withExpr.Message);
        Assert.Equal("x > 0", withExpr.Expression);

        var withInner = new PostconditionViolationException("m", inner);
        Assert.Equal("m", withInner.Message);
        Assert.Same(inner, withInner.InnerException);
    }

    [Fact]
    public void Ensure_and_Invariant_failures_derive_from_AssertionFailedException()
    {
        Assert.IsAssignableFrom<AssertionFailedException>(
            Assert.Throws<PostconditionViolationException>(() => Ensure.That(false)));
        Assert.IsAssignableFrom<AssertionFailedException>(
            Assert.Throws<InvariantViolationException>(() => Invariant.Check(false)));
        Assert.IsAssignableFrom<AssertionFailedException>(Invariant.Unreachable());
    }

    // [StackTraceHidden]: helper frames must not show up; the caller's frame must.
    [Fact]
    public void Helper_frames_are_hidden_from_stack_traces()
    {
        var thrown = new (string Helper, Exception Ex)[]
        {
            ("Guard.NotNull", Catch(() => Guard.NotNull<object>(null))),
            ("Guard.NotNullOrEmpty", Catch(() => Guard.NotNullOrEmpty(""))),
            ("Guard.NotNullOrWhiteSpace", Catch(() => Guard.NotNullOrWhiteSpace(" "))),
            ("Guard.NotEmpty", Catch(() => Guard.NotEmpty(Array.Empty<int>()))),
            ("Guard.NotDefault", Catch(() => Guard.NotDefault(0))),
            ("Guard.Positive", Catch(() => Guard.Positive(0))),
            ("Guard.NotNegative", Catch(() => Guard.NotNegative(-1))),
            ("Guard.InRange", Catch(() => Guard.InRange(5, 0, 1))),
            ("Guard.Defined", Catch(() => Guard.Defined((DayOfWeek)99))),
            ("Guard.Requires", Catch(() => Guard.Requires(false))),
            ("Ensure.That", Catch(() => Ensure.That(false))),
            ("Ensure.NotNull", Catch(() => Ensure.NotNull<object>(null))),
            ("Invariant.Check", Catch(() => Invariant.Check(false))),
#if DEBUG
            ("DebugCheck.That", Catch(() => DebugCheck.That(false))),
#endif
        };

        foreach (var (helper, ex) in thrown)
        {
            var trace = ex.StackTrace ?? string.Empty;
            Assert.DoesNotContain("Assertions." + helper, trace, StringComparison.Ordinal);
            Assert.Contains(nameof(Helper_frames_are_hidden_from_stack_traces), trace, StringComparison.Ordinal);
        }
    }

    private static Exception Catch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected the action to throw.");
    }
}
