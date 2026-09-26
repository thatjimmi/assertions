// Assertions for .NET — fail-fast guards, postconditions and invariants.
// Single-file drop-in. Requires .NET 8 or later. See README.md for usage and rules.
// Copyright (c) 2026 Jimmi Mortensen. Licensed under the MIT License.
// SPDX-License-Identifier: MIT

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

// Change this to match your project, e.g. MyApp.Assertions.
namespace Assertions;

/// <summary>
/// Preconditions: validate arguments at the top of public methods and constructors.
/// </summary>
/// <remarks>
/// <para>
/// Every method throws a standard <see cref="ArgumentException"/>-family exception, so existing
/// error handling keeps working. The parameter name is captured automatically from the expression
/// you pass, so <c>Guard.NotNull(order.Customer)</c> reports <c>order.Customer</c>.
/// </para>
/// <para>
/// Methods that validate a single value return it, so you can validate and assign in one line:
/// <c>_name = Guard.NotNullOrWhiteSpace(name);</c>
/// </para>
/// <para>
/// Guards are for programming errors. Expected invalid input from users (a malformed email, an
/// out-of-range form value) should produce a normal validation result instead.
/// </para>
/// </remarks>
public static class Guard
{
    /// <summary>Requires <paramref name="value"/> to be non-null.</summary>
    /// <typeparam name="T">The reference type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to be non-null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    [StackTraceHidden]
    public static T NotNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>Requires <paramref name="value"/> to be non-null and not empty.</summary>
    /// <param name="value">The string to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to be non-null and non-empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
    [StackTraceHidden]
    public static string NotNullOrEmpty(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
        return value;
    }

    /// <summary>Requires <paramref name="value"/> to be non-null and contain at least one non-whitespace character.</summary>
    /// <param name="value">The string to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to contain non-whitespace text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or whitespace.</exception>
    [StackTraceHidden]
    public static string NotNullOrWhiteSpace(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }

    /// <summary>Requires a collection to be non-null and contain at least one item.</summary>
    /// <remarks>
    /// Collections implementing <see cref="ICollection"/> are checked via their count. Other
    /// sequences are checked by starting to enumerate them, so prefer passing materialized
    /// collections (arrays, lists, sets) rather than lazy LINQ queries.
    /// </remarks>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="value">The collection to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to be non-null and non-empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> contains no items.</exception>
    [StackTraceHidden]
    public static TCollection NotEmpty<TCollection>(
        [NotNull] TCollection? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TCollection : class, IEnumerable
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        if (!HasAny(value))
        {
            throw new ArgumentException("Collection must not be empty.", paramName);
        }

        return value;
    }

    /// <summary>
    /// Requires a value type to differ from its default, for example <see cref="Guid.Empty"/>
    /// or <c>default(DateTime)</c>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known not to be the default.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> equals <c>default(T)</c>.</exception>
    [StackTraceHidden]
    public static T NotDefault<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct
    {
        if (EqualityComparer<T>.Default.Equals(value, default))
        {
            throw new ArgumentException($"Value must not be the default value of {typeof(T).Name}.", paramName);
        }

        return value;
    }

    /// <summary>Requires a number to be greater than zero.</summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The number to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to be positive.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero or negative.</exception>
    [StackTraceHidden]
    public static T Positive<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : INumberBase<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
        return value;
    }

    /// <summary>Requires a number to be zero or greater.</summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The number to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to be non-negative.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    [StackTraceHidden]
    public static T NotNegative<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : INumberBase<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
        return value;
    }

    /// <summary>Requires <paramref name="min"/> &lt;= <paramref name="value"/> &lt;= <paramref name="max"/> (both bounds inclusive).</summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The smallest allowed value.</param>
    /// <param name="max">The largest allowed value.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to be within the range.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is outside the range.</exception>
    [StackTraceHidden]
    public static T InRange<T>(
        T value,
        T min,
        T max,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, min, paramName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, max, paramName);
        return value;
    }

    /// <summary>
    /// Requires an enum value to be one of the enum's declared members, catching casts such as <c>(Status)99</c>.
    /// </summary>
    /// <remarks>
    /// Not suitable for <see cref="FlagsAttribute"/> enums: combinations of flags are valid values
    /// but are not individually declared members, so they would be rejected.
    /// </remarks>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The enum value to check.</param>
    /// <param name="paramName">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="value"/>, known to be a declared member.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a declared member.</exception>
    [StackTraceHidden]
    public static TEnum Defined<TEnum>(
        TEnum value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value is not a defined {typeof(TEnum).Name}.");
        }

        return value;
    }

    /// <summary>
    /// General-purpose precondition for anything the specific guards don't cover.
    /// <c>Guard.Requires(start &lt; end)</c> fails with "Precondition failed: start &lt; end".
    /// </summary>
    /// <param name="condition">The condition that must be true.</param>
    /// <param name="message">
    /// Optional message. Usually unnecessary because the expression is reported automatically.
    /// Avoid interpolated messages in hot paths: they are built on every call, not just on failure.
    /// </param>
    /// <param name="expression">Captured automatically from the condition expression. Do not pass.</param>
    /// <exception cref="ArgumentException"><paramref name="condition"/> is <see langword="false"/>.</exception>
    [StackTraceHidden]
    public static void Requires(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message ?? $"Precondition failed: {expression}");
        }
    }

    private static bool HasAny(IEnumerable source)
    {
        if (source is ICollection collection)
        {
            return collection.Count > 0;
        }

        var enumerator = source.GetEnumerator();
        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }
}

/// <summary>
/// Postconditions: check that a method produced a sensible result before returning it.
/// </summary>
/// <remarks>
/// A failing postcondition means the method itself has a bug, so it throws
/// <see cref="PostconditionViolationException"/> rather than an argument exception.
/// </remarks>
public static class Ensure
{
    /// <summary>Requires a condition about the method's result or final state to be true.</summary>
    /// <param name="condition">The condition that must be true.</param>
    /// <param name="message">
    /// Optional message. Usually unnecessary because the expression is reported automatically.
    /// Avoid interpolated messages in hot paths: they are built on every call, not just on failure.
    /// </param>
    /// <param name="expression">Captured automatically from the condition expression. Do not pass.</param>
    /// <exception cref="PostconditionViolationException"><paramref name="condition"/> is <see langword="false"/>.</exception>
    [StackTraceHidden]
    public static void That(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new PostconditionViolationException(message ?? $"Postcondition failed: {expression}", expression);
        }
    }

    /// <summary>Requires a result to be non-null. Typical use: <c>return Ensure.NotNull(result);</c></summary>
    /// <typeparam name="T">The reference type of the result.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="expression">Captured automatically from the argument expression. Do not pass.</param>
    /// <returns><paramref name="result"/>, known to be non-null.</returns>
    /// <exception cref="PostconditionViolationException"><paramref name="result"/> is <see langword="null"/>.</exception>
    [StackTraceHidden]
    public static T NotNull<T>(
        [NotNull] T? result,
        [CallerArgumentExpression(nameof(result))] string? expression = null)
        where T : class
    {
        if (result is null)
        {
            throw new PostconditionViolationException($"Postcondition failed: {expression} was null", expression);
        }

        return result;
    }
}

/// <summary>
/// Invariants: rules about program state that must always hold, and code paths that must never run.
/// </summary>
public static class Invariant
{
    /// <summary>Requires a rule about the program's state to be true.</summary>
    /// <remarks>
    /// Check invariants after state changes, but prevent violations with <see cref="Guard"/>
    /// preconditions before mutating: an invariant that fires after a mutation reports the bug
    /// but leaves the object in the broken state.
    /// </remarks>
    /// <param name="condition">The condition that must be true.</param>
    /// <param name="message">
    /// Optional message. Usually unnecessary because the expression is reported automatically.
    /// Avoid interpolated messages in hot paths: they are built on every call, not just on failure.
    /// </param>
    /// <param name="expression">Captured automatically from the condition expression. Do not pass.</param>
    /// <exception cref="InvariantViolationException"><paramref name="condition"/> is <see langword="false"/>.</exception>
    [StackTraceHidden]
    public static void Check(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new InvariantViolationException(message ?? $"Invariant failed: {expression}", expression);
        }
    }

    /// <summary>
    /// Creates the exception for a code path that should be impossible. Use it with <c>throw</c> so the
    /// compiler knows the branch does not continue:
    /// <code>_ => throw Invariant.Unreachable($"Unhandled status {status}")</code>
    /// </summary>
    /// <param name="message">Optional detail, such as the unexpected value.</param>
    /// <param name="member">Captured automatically. Do not pass.</param>
    /// <param name="file">Captured automatically. Do not pass.</param>
    /// <param name="line">Captured automatically. Do not pass.</param>
    /// <returns>An exception describing where the impossible code was reached.</returns>
    public static InvariantViolationException Unreachable(
        string? message = null,
        [CallerMemberName] string? member = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var location = $"{member} ({Path.GetFileName(file)}:{line})";
        return new InvariantViolationException(
            message is null
                ? $"Unreachable code reached in {location}"
                : $"Unreachable code reached in {location}: {message}");
    }
}

/// <summary>
/// Expensive or purely diagnostic checks that run only in builds where <c>DEBUG</c> is defined.
/// </summary>
/// <remarks>
/// Calls are removed by the compiler in Release builds, <b>including evaluation of the arguments</b>.
/// That makes them free in production, and it is also why nothing with a side effect may ever be
/// placed inside one: the program would behave differently in Debug and Release.
/// Use <see cref="Invariant.Check"/> for anything that must be enforced in production.
/// </remarks>
public static class DebugCheck
{
    /// <summary>Requires a condition to be true, in Debug builds only.</summary>
    /// <param name="condition">The condition that must be true. Must not have side effects.</param>
    /// <param name="message">Optional message. Usually unnecessary because the expression is reported automatically.</param>
    /// <param name="expression">Captured automatically from the condition expression. Do not pass.</param>
    /// <exception cref="InvariantViolationException"><paramref name="condition"/> is <see langword="false"/>.</exception>
    [Conditional("DEBUG")]
    [StackTraceHidden]
    public static void That(
        bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new InvariantViolationException(message ?? $"Debug check failed: {expression}", expression);
        }
    }
}

/// <summary>
/// Base type for assertion failures raised by <see cref="Ensure"/>, <see cref="Invariant"/> and <see cref="DebugCheck"/>.
/// </summary>
/// <remarks>
/// An assertion failure means the program has a bug. Catch this type only in a top-level handler
/// that logs the failure and fails the current request or job; never catch it to carry on.
/// (<see cref="Guard"/> throws standard argument exceptions instead.)
/// </remarks>
public abstract class AssertionFailedException : Exception
{
    /// <summary>Initializes a new instance with a default message.</summary>
    protected AssertionFailedException()
        : base("An assertion failed.")
    {
    }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The message describing the failure.</param>
    protected AssertionFailedException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified message and failed expression.</summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="expression">The source text of the condition that failed, if known.</param>
    protected AssertionFailedException(string message, string? expression)
        : base(message)
    {
        Expression = expression;
    }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    protected AssertionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// The source text of the condition that failed, such as <c>from.Balance &gt;= 0</c>,
    /// or <see langword="null"/> if not available. Useful as a structured logging field.
    /// </summary>
    public string? Expression { get; }
}

/// <summary>Thrown when an <see cref="Invariant"/> or <see cref="DebugCheck"/> fails, or unreachable code is reached.</summary>
public sealed class InvariantViolationException : AssertionFailedException
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public InvariantViolationException()
    {
    }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The message describing the failure.</param>
    public InvariantViolationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified message and failed expression.</summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="expression">The source text of the condition that failed, if known.</param>
    public InvariantViolationException(string message, string? expression)
        : base(message, expression)
    {
    }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public InvariantViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Thrown when an <see cref="Ensure"/> postcondition fails.</summary>
public sealed class PostconditionViolationException : AssertionFailedException
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public PostconditionViolationException()
    {
    }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The message describing the failure.</param>
    public PostconditionViolationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified message and failed expression.</summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="expression">The source text of the condition that failed, if known.</param>
    public PostconditionViolationException(string message, string? expression)
        : base(message, expression)
    {
    }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public PostconditionViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
