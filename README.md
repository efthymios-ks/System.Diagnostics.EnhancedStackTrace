# System.Diagnostics.EnhancedStackTrace

A stack trace with the compiler's rewriting undone: real names for lambdas, local functions, async
methods and iterators, C# type names, and no state-machine plumbing. A demo, not a package — clone
it and copy what is useful.

```
EnhancedStackTrace.cs      Create / Current / Describe, IReadOnlyList<EnhancedStackFrame>
EnhancedStackFrame.cs      MethodName, DeclaringTypeName, Parameters, Kind, FileName, LineNumber
StackFrameParameter.cs     TypeName, Name, Modifier, DefaultValue
MethodKind.cs              Method, Constructor, Lambda, LocalFunction, AsyncOrIterator, …
ExceptionExtensions.cs     GetEnhancedStackTrace / ToEnhancedString
Formatting/                type aliasing and compiler-name parsing
```

## Read a trace

```csharp
catch (Exception exception)
{
    Console.WriteLine(exception.ToEnhancedString());
}
```

Real output, paths shortened. A lambda:

```
System.InvalidOperationException: lambda
   at Throwers.<>c.<FromLambda>b__2_0() in Throwers.cs:line 14
   at Throwers.FromLambda() in Throwers.cs:line 16
   at Throwers.Catch(Action action) in Throwers.cs:line 67
```

```
System.InvalidOperationException: lambda
   at void Throwers.FromLambda()+lambda in Throwers.cs:line 14
   at void Throwers.FromLambda() in Throwers.cs:line 16
   at Exception Throwers.Catch(Action action) in Throwers.cs:line 67
```

An async method, where the runtime reports `MoveNext` with no signature of its own:

```
System.InvalidOperationException: async
   at Throwers.FromAsyncMethodAsync() in Throwers.cs:line 31
   at Throwers.CatchAsync(Func`1 action) in Throwers.cs:line 81
```

```
System.InvalidOperationException: async
   at Task Throwers.FromAsyncMethodAsync() in Throwers.cs:line 31
   at Task<Exception> Throwers.CatchAsync(Func<Task> action) in Throwers.cs:line 81
```

Return types appear, `Func\`1` becomes `Func<Task>`, and a lambda is told apart from the method
that declares it.

## Work with frames

Frames are data, not a formatted line, so you can filter before printing.

```csharp
var trace = exception.GetEnhancedStackTrace();

var mine = trace.Where(frame => frame.DeclaringTypeName.StartsWith("MyApp"));
var throwing = trace[0];

Console.WriteLine($"{throwing.MethodName} at line {throwing.LineNumber}");
```

| Member | Reports |
| --- | --- |
| `MethodName` | the name as written in source — the property's name for an accessor |
| `OwnerMethodName` | the method a lambda or local function sits inside |
| `Kind` | `Method`, `Constructor`, `Lambda`, `LocalFunction`, `AsyncOrIterator`, `PropertyGetter`, `PropertySetter`, `Indexer` |
| `DeclaringTypeName`, `ReturnTypeName` | C# names — `int`, `List<string>`, `Outer.Inner`, `(int, string)` |
| `Parameters` | type, name, `ref`/`out`/`in`/`params`, default value |
| `FileName`, `LineNumber`, `HasSource` | present only with debug symbols |
| `Method` | the underlying `MethodBase` |

## Where a trace comes from

| Call | Gives |
| --- | --- |
| `EnhancedStackTrace.Create(exception)` | the exception's trace |
| `EnhancedStackTrace.Create(stackTrace)` | an existing `StackTrace`, enhanced |
| `EnhancedStackTrace.Current()` | the caller's own stack |
| `EnhancedStackTrace.Describe(exception)` | message and trace, plus every inner exception's |

`Describe` walks `AggregateException.InnerExceptions`, so nothing is lost when several failures are
bundled together.

## What is dropped

Frames marked `[StackTraceHidden]`, and anything in `System.Runtime.CompilerServices` or
`System.Runtime.ExceptionServices` — await machinery and rethrow points say nothing about your code.

## Enhance in place

`Create` and `Describe` leave the exception alone. `Enhance` rewrites its `StackTrace`, and every
inner exception's, so code that already calls `ToString` prints the enhanced trace without being
touched.

```csharp
catch (Exception exception)
{
    logger.LogError(exception.Enhance(), "Order {Id} failed", id);
}
```

Safe to call twice — the trace is rebuilt from the frames the runtime recorded, not from the string
it replaced. It writes to `Exception._stackTraceString`, a private runtime field: if a future
runtime renames it, `Enhance` becomes a no-op and `CanEnhanceInPlace` reports false. A test pins the
field's existence so that shows up as a failure rather than as silence.

## File and line numbers

They appear only when the assembly was built with symbols. Set `DebugType` in the csproj:

```xml
<PropertyGroup>
  <DebugType>portable</DebugType>
</PropertyGroup>
```

`portable` writes a .pdb next to the assembly; `embedded` puts the same information inside it.

## How frames render

| Shape | Reads as |
| --- | --- |
| method | `void Orders.Load(int id, bool full = true)` |
| constructor | `new Widget()` |
| property | `string Widget.Label.get`, `void Meter.Reading.set` |
| indexer | `string Widget[int index]` |
| lambda | `void Throwers.FromLambda()+lambda` |
| local function | `void Throwers.FromLocalFunction()+Helper()` |
| async / iterator | `Task Orders.FetchAsync(int id)`, `IEnumerable<int> Orders.All()` |

## Limits

A stack frame carries the generic *definition*, so a call to `Load<int>` reports `<TValue>`, not
`<int>` — the runtime does not record which arguments were used.
