namespace System.Diagnostics.EnhancedStackTrace.Tests.Shared;

/// <summary>Each of these throws from a differently shaped frame, so the naming can be checked.</summary>
public static class Throwers
{
    public static void FromMethod()
        => throw new InvalidOperationException("method");

    public static int FromGenericMethod<TValue>(TValue value)
        => throw new InvalidOperationException($"generic {value}");

    public static void FromLambda()
    {
        Action action = () => throw new InvalidOperationException("lambda");

        action();
    }

    public static void FromLocalFunction()
    {
        Helper();

        static void Helper()
            => throw new InvalidOperationException("local function");
    }

    public static async Task FromAsyncMethodAsync()
    {
        await Task.Yield();

        throw new InvalidOperationException("async");
    }

    public static void FromNestedCall()
        => FromMethod();

    public static void FromParameters(int count, string name, out bool flag)
    {
        flag = false;

        throw new InvalidOperationException("parameters");
    }

    public static IEnumerable<int> FromIterator()
    {
        yield return 1;

        throw new InvalidOperationException("iterator");
    }

    public static void FromDefaults(int count = 5, string name = "x", bool flag = true)
        => throw new InvalidOperationException("defaults");

    public static void FromParams(params int[] values)
        => throw new InvalidOperationException("params");

    public static void FromTuple((int Left, string Right) pair)
        => throw new InvalidOperationException("tuple");

    public static void FromNestedType(Outer.Inner inner)
        => throw new InvalidOperationException("nested");

    public static void FromRefAndIn(ref int counter, in double rate)
        => throw new InvalidOperationException("ref and in");

    public static void FromNullable(int? maybe, DateTime? when)
        => throw new InvalidOperationException("nullable");

    public static void FromTupleClass(Tuple<int, string> pair)
        => throw new InvalidOperationException("tuple class");

    public static void FromMatrix(int[,] grid, nint handle)
        => throw new InvalidOperationException("matrix");

    public static void FromGenericHolder(Holder<int, string> holder)
        => throw new InvalidOperationException("generic holder");

    [StackTraceHidden]
    public static void Hidden()
        => FromMethod();

    public static void FromHiddenWrapper()
        => Hidden();

    public static Exception Catch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("the action did not throw");
    }

    public static async Task<Exception> CatchAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("the action did not throw");
    }
}

public sealed class Widget
{
    public Widget()
        => throw new InvalidOperationException("constructor");

    public Widget(int size)
        => Size = size;

    public int Size { get; }

    public string this[int index]
        => throw new InvalidOperationException("indexer");

    public string Label
        => throw new InvalidOperationException("getter");
}

public sealed class Box<TItem>
{
    public TItem Open()
        => throw new InvalidOperationException("generic type");
}

public sealed class Outer
{
    public sealed class Inner;
}

public sealed class Meter
{
    public int Reading
    {
        get => 1;
        set => throw new InvalidOperationException("setter");
    }
}

public sealed class Holder<TFirst, TSecond>;

public static class FailingInitialiser
{
    static FailingInitialiser()
        => throw new InvalidOperationException("static constructor");

    public static int Value { get; } = 1;
}
