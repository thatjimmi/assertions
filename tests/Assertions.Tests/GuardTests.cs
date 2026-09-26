namespace Assertions.Tests;

public class GuardTests
{
    private enum Color { Red = 1, Green = 2 }

    private sealed class Order { public string? Customer { get; set; } }

    // NotNull
    [Fact]
    public void NotNull_returns_same_instance()
    {
        var o = new object();
        Assert.Same(o, Guard.NotNull(o));
    }

    [Fact]
    public void NotNull_throws_ArgumentNullException_with_expression_as_ParamName()
    {
        var order = new Order();
        var ex = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(order.Customer));
        Assert.Equal("order.Customer", ex.ParamName);
    }

    // NotNullOrEmpty / NotNullOrWhiteSpace
    [Fact]
    public void NotNullOrEmpty_success_returns_same_string()
    {
        var s = "x";
        Assert.Same(s, Guard.NotNullOrEmpty(s));
        Assert.Equal(" ", Guard.NotNullOrEmpty(" "));
    }

    [Fact]
    public void NotNullOrEmpty_failures()
    {
        string? name = null;
        var nullEx = Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrEmpty(name));
        Assert.Equal("name", nullEx.ParamName);

        var empty = "";
        var ex = Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(empty));
        Assert.Equal("empty", ex.ParamName);
    }

    [Fact]
    public void NotNullOrWhiteSpace_success_returns_same_string()
    {
        var s = " a ";
        Assert.Same(s, Guard.NotNullOrWhiteSpace(s));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void NotNullOrWhiteSpace_throws_ArgumentException_for_blank(string blank)
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace(blank));
        Assert.Equal("blank", ex.ParamName);
    }

    [Fact]
    public void NotNullOrWhiteSpace_throws_ArgumentNullException_for_null()
    {
        string? customerName = null;
        var ex = Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrWhiteSpace(customerName));
        Assert.Equal("customerName", ex.ParamName);
    }

    // NotEmpty
    [Fact]
    public void NotEmpty_success_for_list_array_set_and_lazy_returns_same_instance()
    {
        var list = new List<int> { 1 };
        var array = new[] { 1 };
        var set = new HashSet<int> { 1 };
        IEnumerable<int> lazy = Lazy();

        Assert.Same(list, Guard.NotEmpty(list));
        Assert.Same(array, Guard.NotEmpty(array));
        Assert.Same(set, Guard.NotEmpty(set));
        Assert.Same(lazy, Guard.NotEmpty(lazy));
    }

    [Fact]
    public void NotEmpty_failure_for_empty_list_array_set_and_lazy()
    {
        var list = new List<int>();
        var array = Array.Empty<int>();
        var set = new HashSet<int>();
        var lazy = LazyEmpty();

        Assert.Equal("list", Assert.Throws<ArgumentException>(() => Guard.NotEmpty(list)).ParamName);
        Assert.Equal("array", Assert.Throws<ArgumentException>(() => Guard.NotEmpty(array)).ParamName);
        Assert.Equal("set", Assert.Throws<ArgumentException>(() => Guard.NotEmpty(set)).ParamName);
        Assert.Equal("lazy", Assert.Throws<ArgumentException>(() => Guard.NotEmpty(lazy)).ParamName);
    }

    [Fact]
    public void NotEmpty_null_throws_ArgumentNullException()
    {
        List<int>? items = null;
        var ex = Assert.Throws<ArgumentNullException>(() => Guard.NotEmpty(items));
        Assert.Equal("items", ex.ParamName);
    }

    [Fact]
    public void NotEmpty_lazy_disposes_enumerator_and_stops_after_first_item()
    {
        var pulled = 0;
        var disposed = false;

        IEnumerable<int> Source()
        {
            try
            {
                pulled++;
                yield return 1;
                pulled++;
                yield return 2;
            }
            finally
            {
                disposed = true;
            }
        }

        Guard.NotEmpty(Source());

        Assert.Equal(1, pulled);
        Assert.True(disposed);
    }

    // NotDefault
    [Fact]
    public void NotDefault_success_returns_value()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, Guard.NotDefault(id));
        Assert.Equal(1, Guard.NotDefault(1));
    }

    [Fact]
    public void NotDefault_throws_ArgumentException_for_default()
    {
        var id = Guid.Empty;
        var ex = Assert.Throws<ArgumentException>(() => Guard.NotDefault(id));
        Assert.Equal("id", ex.ParamName);

        var when = default(DateTime);
        Assert.Throws<ArgumentException>(() => Guard.NotDefault(when));
        Assert.Throws<ArgumentException>(() => Guard.NotDefault(0));
    }

    // Positive / NotNegative
    [Fact]
    public void Positive_success_and_failure()
    {
        Assert.Equal(1, Guard.Positive(1));
        Assert.Equal(0.5m, Guard.Positive(0.5m));

        var zero = 0;
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(zero));
        Assert.Equal("zero", ex.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(-1m));
    }

    [Fact]
    public void NotNegative_success_and_failure()
    {
        Assert.Equal(0, Guard.NotNegative(0));
        Assert.Equal(2.5, Guard.NotNegative(2.5));

        var negative = -1;
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NotNegative(negative));
        Assert.Equal("negative", ex.ParamName);
    }

    // InRange
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void InRange_accepts_inclusive_bounds_and_interior(int value)
    {
        Assert.Equal(value, Guard.InRange(value, 0, 10));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void InRange_rejects_just_outside_bounds(int value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(value, 0, 10));
        Assert.Equal("value", ex.ParamName);
    }

    // Defined
    [Fact]
    public void Defined_success_returns_value()
    {
        Assert.Equal(Color.Green, Guard.Defined(Color.Green));
    }

    [Fact]
    public void Defined_throws_for_undefined_cast()
    {
        var color = (Color)99;
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Defined(color));
        Assert.Equal("color", ex.ParamName);
        Assert.Equal(color, ex.ActualValue);
    }

    // Requires
    [Fact]
    public void Requires_true_does_not_throw()
    {
        Guard.Requires(1 < 2);
    }

    [Fact]
    public void Requires_false_throws_exact_ArgumentException_with_expression_message()
    {
        var start = 5;
        var end = 3;
        var ex = Assert.Throws<ArgumentException>(() => Guard.Requires(start < end));
        Assert.Equal("Precondition failed: start < end", ex.Message);
        Assert.Null(ex.ParamName);
    }

    [Fact]
    public void Requires_custom_message_replaces_expression()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.Requires(false, "custom"));
        Assert.Equal("custom", ex.Message);
    }

    // Guard failures are not assertion failures
    [Fact]
    public void Guard_failures_are_not_AssertionFailedException()
    {
        Exception[] all =
        [
            Assert.ThrowsAny<Exception>(() => Guard.NotNull<object>(null)),
            Assert.ThrowsAny<Exception>(() => Guard.Positive(0)),
            Assert.ThrowsAny<Exception>(() => Guard.Requires(false)),
        ];
        Assert.All(all, e => Assert.IsNotAssignableFrom<AssertionFailedException>(e));
        Assert.All(all, e => Assert.IsAssignableFrom<ArgumentException>(e));
    }

    private static IEnumerable<int> Lazy()
    {
        yield return 1;
    }

    private static IEnumerable<int> LazyEmpty()
    {
        yield break;
    }
}
