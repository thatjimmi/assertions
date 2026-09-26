# Assertions for .NET

Fail-fast guards, postconditions and invariants for C#, in one file you copy into your project. No package, no dependencies.

```csharp
public void Withdraw(decimal amount)
{
    Guard.Positive(amount);
    Guard.Requires(amount <= Balance);

    Balance -= amount;

    Invariant.Check(Balance >= 0);
}
```

When a check fails, the error names the exact expression that broke:

```
System.ArgumentException: Precondition failed: amount <= Balance
```

- [Why use assertions](#why-use-assertions)
- [Installation](#installation)
- [The four helpers](#the-four-helpers)
- [The rules](#the-rules)
- [Validation is not assertion](#validation-is-not-assertion)
- [Prevent, then verify](#prevent-then-verify)
- [When an assertion fails](#when-an-assertion-fails)
- [Testing](#testing)
- [Running the tests](#running-the-tests)
- [Performance](#performance)
- [Beyond assertions](#beyond-assertions)
- [Using with AI coding agents](#using-with-ai-coding-agents)
- [FAQ](#faq)

---

## Why use assertions

Most bugs don't crash where they're born. A null customer, a negative balance or an invalid state slips through, travels through several more methods, and surfaces as a confusing error somewhere unrelated — or worse, as bad data in your database that nobody notices for weeks.

Assertions make bugs **fail loudly, immediately, at the source**, with a message pointing at the rule that was broken:

```
Invariant failed: from.Balance + to.Balance == totalBefore
```

The idea comes from safety-critical software, design-by-contract and the "fail fast" principle. It's inspired by NASA/JPL's [Power of 10 rules](https://en.wikipedia.org/wiki/The_Power_of_10:_Rules_for_Developing_Safety-Critical_Code), written for C: this file implements their assertion rule and the parameter-checking half of the "check parameters and return values" rule for C#, and [Beyond assertions](#beyond-assertions) covers one more practice worth adopting. Assertions also document intent: a reader sees what a method assumes and promises without reading its implementation.

They matter even more when AI agents write part of your code:

- **They check every execution, not just every diff.** A plausible-looking change that quietly breaks a rule gets caught the first time it runs, even if the review missed it.
- **They give precise feedback.** "Precondition failed: start < end" lets an agent (or a person) fix the real problem on the first try instead of guessing.
- **They turn property-based tests into logic-bug finders.** Random inputs alone only find crashes; random inputs plus internal invariants find wrong answers.
- **They can't be forgotten.** A rule in documentation is a suggestion. An `Invariant.Check` is enforced every time, whoever wrote the change.

---

## Installation

**Requirements:** .NET 8 or later.

1. Copy `Assertions.cs` into your project. If you have several projects, put it in a shared class library that the others reference, so the types exist only once.
2. Change the namespace at the top of the file to match your project, for example `MyApp.Assertions`.
3. Optionally make it available in every file by adding this to your `.csproj`:
   ```xml
   <ItemGroup>
     <Using Include="MyApp.Assertions" />
   </ItemGroup>
   ```

The file enables nullable annotations itself and declares all its `using`s, so it compiles in projects with or without nullable reference types and implicit usings. Every public member has XML documentation, so IntelliSense explains each method.

---

## The four helpers

| Class        | Purpose                                  | Where                                    | On in Release?           | Throws                            |
| ------------ | ---------------------------------------- | ---------------------------------------- | ------------------------ | --------------------------------- |
| `Guard`      | **Preconditions**: validate arguments    | Top of public methods and constructors   | Yes                      | `ArgumentException` family        |
| `Ensure`     | **Postconditions**: validate results     | Just before returning                    | Yes                      | `PostconditionViolationException` |
| `Invariant`  | **Invariants** and impossible code paths | After state changes; `default` branches  | Yes                      | `InvariantViolationException`     |
| `DebugCheck` | **Expensive diagnostics**                | Where a check is too slow for production | **No**, removed entirely | `InvariantViolationException`     |

Every helper captures the expression you pass, so you rarely need to write a message. `Ensure`, `Invariant` and `DebugCheck` failures derive from `AssertionFailedException`, which exposes the failed `Expression` for structured logging.

### Guard

Validate data where it enters your code. Single-value methods return the value, so you can validate and assign in one line:

```csharp
public Order(Guid id, string customerName, List<OrderLine> lines)
{
    Id = Guard.NotDefault(id);
    CustomerName = Guard.NotNullOrWhiteSpace(customerName);
    Lines = Guard.NotEmpty(lines);
}
```

| Method                   | Requires                                                  |
| ------------------------ | --------------------------------------------------------- |
| `NotNull(x)`             | not null                                                  |
| `NotNullOrEmpty(s)`      | string is not null or empty                               |
| `NotNullOrWhiteSpace(s)` | string contains non-whitespace text                       |
| `NotEmpty(items)`        | collection is not null and has at least one item          |
| `NotDefault(x)`          | value type is not its default, e.g. `Guid.Empty`          |
| `Positive(n)`            | number > 0                                                |
| `NotNegative(n)`         | number >= 0                                               |
| `InRange(n, min, max)`   | min <= n <= max, both inclusive                           |
| `Defined(e)`             | enum value is a declared member (not for `[Flags]` enums) |
| `Requires(condition)`    | anything else, e.g. `Guard.Requires(start < end)`         |

`Guard` throws the standard .NET argument exceptions, so existing error handling keeps working. The exception's parameter name is the expression you passed: `Guard.NotNull(order.Customer)` reports `order.Customer`.

### Ensure

Check that a method produced a sensible result before handing it back:

```csharp
public decimal ApplyDiscount(decimal percent)
{
    Guard.InRange(percent, 0m, 100m);
    var before = Total;

    Total -= Total * percent / 100m;

    Ensure.That(Total >= 0 && Total <= before);
    return Total;
}
```

`return Ensure.NotNull(result);` is handy for methods that must never return null.

### Invariant

Rules about your program's state that must always hold, checked right after state changes. Conservation rules ("the total before equals the total after") and range rules ("never negative") are the most valuable:

```csharp
Invariant.Check(from.Balance + to.Balance == totalBefore);
```

`Invariant.Unreachable()` marks code that should be impossible, and records the method, file and line:

```csharp
public static string Label(OrderStatus status) => Guard.Defined(status) switch
{
    OrderStatus.Pending => "Pending",
    OrderStatus.Shipped => "Shipped",
    _ => throw Invariant.Unreachable($"Unhandled status {status}"),
};
```

If someone later adds `OrderStatus.Cancelled` and forgets this switch, the first call fails with a clear message instead of silently returning a wrong value.

### DebugCheck

For checks too slow for production, such as validating a whole data structure inside a loop:

```csharp
DebugCheck.That(IsSorted(items));
```

In Release builds the compiler removes the call **and the evaluation of its arguments**. That makes it free, and it's also why a `DebugCheck` must never contain anything with a side effect.

---

## The rules

1. **Assertions are for bugs, not bad input.** A failing assertion means _the code is wrong_. A user typing an invalid email is not a bug. See [Validation is not assertion](#validation-is-not-assertion).
2. **Guard every public entry point.** Public methods and constructors validate their arguments with `Guard`.
3. **Protect what's expensive to get wrong.** Add `Ensure` and `Invariant` checks where a silent error would be costly: money, permissions, state transitions, anything written to a database. Don't blanket every method.
4. **Prevent before you mutate; verify after.** See [Prevent, then verify](#prevent-then-verify).
5. **Don't check what you just created.** `Guard.NotNull` on an object constructed two lines above is noise, and noise hides the checks that matter.
6. **No side effects inside assertions, ever.** Only read state. `Invariant.Check(queue.TryDequeue(out var x))` changes behavior, and inside `DebugCheck` it would behave differently in Debug and Release.
7. **Never catch assertion failures to carry on.** Only a top-level handler catches `AssertionFailedException`. If one fires, fix the bug instead of silencing the alarm.
8. **A failing assertion in a test means the code is wrong** until proven otherwise. Only change or remove a check if you can explain why the rule itself was wrong.
9. **Prefer the type system when it can do the job.** Nullable reference types and well-designed types (an `EmailAddress` that can only be created valid) remove whole categories of runtime checks. Assertions cover what the compiler can't.

---

## Validation is not assertion

This is the most common mistake.

|           | Validation                          | Assertion                       |
| --------- | ----------------------------------- | ------------------------------- |
| Meaning   | The input is bad                    | The code is wrong               |
| Expected? | Yes, users make mistakes            | No, never in a correct program  |
| Response  | Friendly error, 400, let them retry | Log as a bug, 500, fix the code |
| Tool      | Validation result, `TryX` pattern   | `Guard`, `Ensure`, `Invariant`  |

```csharp
// API boundary: user input. Invalid input is expected, so return a validation problem.
app.MapPost("/customers", (CreateCustomerRequest request) =>
{
    if (!EmailAddress.TryParse(request.Email, out var email))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["email"] = ["Enter a valid email address."],
        });
    }

    var customer = new Customer(email); // Inside the domain, Customer guards against null: that would be a bug.
    return Results.Created($"/customers/{customer.Id}", customer);
});
```

Expected business outcomes aren't assertions either. Insufficient funds is a normal result, so model it as one:

```csharp
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
```

---

## Prevent, then verify

An invariant that fires _after_ a mutation reports the bug but leaves the object broken. Use preconditions so invalid changes never start, and keep invariants as the backstop:

```csharp
public void Withdraw(decimal amount)
{
    Guard.Positive(amount);
    Guard.Requires(amount <= Balance); // Prevent: nothing has changed yet.

    Balance -= amount;

    Invariant.Check(Balance >= 0);     // Verify: catches future bugs.
}
```

For operations spanning several objects or a database, do the work in a transaction so a failure rolls everything back.

---

## When an assertion fails

An assertion failure means the current operation can't be trusted, so stop it deliberately: fail the request or job, log it loudly, and don't write possibly-corrupt data.

### Background jobs and message handlers

Catch `AssertionFailedException` at the top of each work item, log it, mark that item failed (or dead-letter the message), and move on. Don't retry: a bug fails the same way every time.

```csharp
try
{
    await ProcessAsync(item, cancellationToken);
}
catch (AssertionFailedException ex)
{
    logger.LogCritical(ex, "Assertion failed ({Expression}) processing {ItemId}. This is a bug.", ex.Expression, item.Id);
    await MarkFailedAsync(item, cancellationToken);
}
```

---

## Testing

**Test that guards reject bad input.** `Assert.Throws` requires the exact type: `ArgumentNullException`, `ArgumentOutOfRangeException` or `ArgumentException`.

```csharp
[Fact]
public void Withdraw_rejects_non_positive_amounts()
{
    var account = new Account(100m);

    Assert.Throws<ArgumentOutOfRangeException>(() => account.Withdraw(0m));
}
```

**Let property-based tests hunt for invariant violations.** With [FsCheck](https://fscheck.github.io/FsCheck/) (`FsCheck.Xunit` package), random inputs drive your code and the invariants inside it catch wrong results, so the test doesn't need to spell out every expected value:

```csharp
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
```

When FsCheck finds a failure it shrinks the input to the smallest case that breaks. Once fixed, add that case as a regular `[Fact]` so it never comes back.

**Run tests in both Debug and Release** in CI, so `DebugCheck`s are exercised and Release behavior is verified.

---

## Running the tests

This repository tests itself, including the claims in this README:

```
dotnet test                # Debug: DebugCheck throws
dotnet test -c Release     # Release: DebugCheck calls and their arguments are removed
```

The projects in `tests/` link to `Assertions.cs` directly, so they always run against the real file. `Assertions.DropIn` proves the file compiles with nullable and implicit usings disabled, and `Assertions.ReadmeSamples` compiles this README's examples, so the build breaks if they drift from the code.

**If you're copying the library, you only need the `.cs` files, not `tests/`.**

---

## Performance

The helpers cost a comparison and a branch, with no allocation when the check passes. Two things to watch:

- **Messages are evaluated eagerly.** `Invariant.Check(x > 0, $"x was {x}")` builds the string on every call, even when the check passes. The automatic expression text is usually enough, so leave messages out in hot paths.
- **Expensive conditions belong in `DebugCheck`.** Anything that walks a collection or recomputes a result shouldn't run on every production call.

---

## Beyond assertions

Assertions catch at runtime what nothing else caught earlier. One practice, adapted from the rest of the Power of 10 rules, catches problems earlier than assertions can. It's a project setting, not code in this file.

### Treat warnings as errors, with nullable and analyzers on

Add this to every project (or once, in a `Directory.Build.props` at the repository root):

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
</PropertyGroup>
```

The compiler and analyzers catch problems at build time; assertions catch the rest at runtime. Nullable reference types in particular remove a whole category of null checks. A build that fails on warnings also forces AI agents to fix problems rather than leave them behind.

On an existing codebase this can surface many warnings at once. Enable it one project at a time, or start with `<WarningsAsErrors>nullable</WarningsAsErrors>` and widen it as you clean up.

### What we deliberately don't adopt

- **"At least two assertions per function."** That density suits flight software; in application code it creates noise that hides the checks that matter. Put checks where being wrong is expensive.

---

## Using with AI coding agents

To make agents like Claude Code apply these rules consistently, give them two things.

### 1. A section in `CLAUDE.md`

It's loaded into every session, so keep it short. Paste this into your repository's `CLAUDE.md`:

```markdown
## Assertions (C#)

We use Assertions.cs (`Guard`, `Ensure`, `Invariant`, `DebugCheck`). Full guidance is in the `csharp-assertions` skill.

- Public methods and constructors validate arguments with `Guard` (it returns the value: `_name = Guard.NotNullOrWhiteSpace(name);`).
- Code that changes money, permissions, state or persisted data prevents invalid changes with `Guard` _before_ mutating, and verifies with `Invariant.Check` / `Ensure.That` after.
- Impossible branches use `throw Invariant.Unreachable(...)`, never a silent default.
- Assertions are for bugs only. Invalid user input and expected business outcomes (e.g. insufficient funds) are normal results, not assertions.
- No side effects inside any assertion.
- Never catch `AssertionFailedException` except in the top-level handler. If an assertion fails in a test, fix the code; don't weaken the check.
- Don't guard values created in the same method, and don't blanket private helpers with checks.

## Code rules

- The build treats warnings as errors. Fix warnings; never suppress one without a comment explaining why.
```

### 2. A skill with the full guidance

Skills load only when relevant, so they can go into more detail. Save the following as `.claude/skills/csharp-assertions/SKILL.md` in your repository.

<details>
<summary>Show SKILL.md</summary>

```markdown
---
name: csharp-assertions
description: How to add and review guards, postconditions and invariants in C# code using Assertions.cs (Guard, Ensure, Invariant, DebugCheck). Use this skill whenever writing, changing, refactoring or reviewing any C# method, constructor, class or endpoint; whenever code handles money, permissions, state transitions or database writes; whenever a test fails with ArgumentException, InvariantViolationException or PostconditionViolationException; and whenever writing FsCheck property-based tests, even if the task doesn't mention assertions.
---

# C# assertions

This codebase uses Assertions.cs. The goal: bugs fail loudly at their source with a precise message, instead of propagating silently into bad data. Assertions mean "our code is wrong", never "the user made a mistake".

## Which helper

- `Guard.*`: preconditions at the top of public methods and constructors. Throws standard `ArgumentException` types. Single-value methods return the value, so validate and assign together.
  Available: `NotNull`, `NotNullOrEmpty`, `NotNullOrWhiteSpace`, `NotEmpty`, `NotDefault`, `Positive`, `NotNegative`, `InRange` (inclusive), `Defined` (enums, not `[Flags]`), `Requires(condition)` for anything else. Prefer the specific method over `Requires` when one fits.
- `Ensure.That(...)` / `Ensure.NotNull(...)`: postconditions just before returning.
- `Invariant.Check(...)`: rules about state that must always hold, checked right after state changes.
- `throw Invariant.Unreachable(...)`: branches that should be impossible, typically the `_ =>` arm of a switch over an enum or state.
- `DebugCheck.That(...)`: expensive checks only. Removed from Release builds together with its arguments.

The expression text is captured automatically. Don't add messages that restate the condition, and don't use interpolated messages in hot paths (they're built on every call).

## Where to add checks

1. Every public constructor and public method guards its parameters.
2. Anything expensive to get wrong: money, balances, quantities, permissions, state-machine transitions, database writes. Ask "what must be true after this runs?" and check it. Conservation rules and range rules are the most valuable.
3. Switches over enums or states end with `_ => throw Invariant.Unreachable($"Unhandled {nameof(status)} {status}")`.

## Prevent, then verify

Guard everything that would make a change invalid before mutating, then mutate, then check invariants as a backstop. For changes spanning several objects or the database, use a transaction.

## Where NOT to add checks

- User input: return a validation result (e.g. `Results.ValidationProblem(...)`), not an assertion.
- Expected business outcomes (insufficient funds, out of stock, duplicate username): model as a return value (`bool TryX`, a result type).
- Values created in the same method a few lines earlier.
- Private helpers with a single caller that already validated the data.

Aim for a few meaningful checks, not one on every line.

## Hard rules

- No side effects inside assertions. Only read state.
- Never catch `AssertionFailedException` (or its subclasses) outside the top-level handler, and never use try/catch to make one go away.
- A failing assertion in a test means the code under test is wrong. Fix the code. Only change or remove the check if you can explain why the rule was wrong, and say so explicitly in your summary.
- Don't weaken existing checks (loosening a bound, turning `Invariant.Check` into `DebugCheck.That`, deleting a guard) without flagging it.

## Related code rules

- Warnings are errors. Fix them instead of suppressing them.

## Tests

- For each new guard, test that invalid input throws the exact expected exception type.
- For code with invariants, prefer an FsCheck `[Property]` test that drives it with random valid inputs.
- Turn shrunk FsCheck failures into regular `[Fact]` regression tests once fixed.

## Reviewing code

Check that: new public entry points have guards; state changes guard before mutating and verify after; user input and business outcomes are results, not assertions; no assertion has side effects; no assertion exception is caught outside the top-level handler; no existing check was weakened without explanation; no warnings were suppressed without explanation.
```

</details>

When an agent gets something wrong in a new way, add a line to the skill describing the right behavior. That's how the guidance stays accurate.

---

## FAQ

**Why not `Debug.Assert`?** It disappears in Release, so it can't protect production, and in some test runners a failure pops a dialog or kills the process instead of throwing. Here, `DebugCheck` covers the debug-only case with a normal exception, and everything else stays on in Release.

**Why a file instead of a NuGet package?** It's small, stable and dependency-free, and copying it means you own it: rename the namespace, add a helper, change a message, with no version to track. If you use it across many repositories, a shared internal package may suit you better.

**I already use another `Guard` library.** Put this in its own namespace and don't import both in the same file, or rename one of the classes.

**What if my team disagrees with a rule?** Change it. Update the README, the `CLAUDE.md` section and the skill together so people and agents follow the same rules.

## License

MIT. Use it, change it and ship it in commercial or open-source code. The one condition: keep the copyright and license header at the top of each file when you copy it.
