namespace System.Diagnostics.EnhancedStackTrace.Formatting;

/// <summary>
/// What the compiler rewrote a lambda, local function or async method into, read back out.
/// A name like <c>&lt;Run&gt;b__3_0</c> means "lambda 0 inside Run"; a state machine type is named
/// <c>&lt;Run&gt;d__3</c>. Without this the frame reads as machinery rather than as your code.
/// </summary>
internal readonly record struct CompilerName(string Name, string? Owner, MethodKind Kind)
{
    public static CompilerName Parse(string rawName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawName);

        var closeIndex = rawName.IndexOf('>');

        if (closeIndex < 0)
        {
            return new CompilerName(StripGenericArity(rawName), Owner: null, MethodKind.Method);
        }

        var openIndex = rawName.LastIndexOf('<', closeIndex);

        if (openIndex < 0)
        {
            return new CompilerName(StripGenericArity(rawName), Owner: null, MethodKind.Method);
        }

        var owner = StripGenericArity(rawName[(openIndex + 1)..closeIndex]);
        var suffix = rawName[(closeIndex + 1)..];
        var kind = KindOf(suffix);

        // A state machine or a closure has no name of its own; the method it came from is the name.
        var inner = InnerName(suffix);

        return inner is null
            ? new CompilerName(owner, Owner: null, kind)
            : new CompilerName(inner, owner, kind);
    }

    private static MethodKind KindOf(string suffix)
        => suffix.Length == 0 ? MethodKind.Method : suffix[0] switch
        {
            'b' => MethodKind.Lambda,
            'g' => MethodKind.LocalFunction,
            'd' => MethodKind.AsyncOrIterator,
            'c' => MethodKind.Lambda,
            _ => MethodKind.Method
        };

    /// <summary>
    /// A local function carries its own name after the pipe (<c>g__Helper|3_0</c>); a lambda does not.
    /// </summary>
    private static string? InnerName(string suffix)
    {
        const string separator = "__";

        var separatorIndex = suffix.IndexOf(separator, StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            return null;
        }

        var afterSeparator = suffix[(separatorIndex + separator.Length)..];
        var pipeIndex = afterSeparator.IndexOf('|');

        if (pipeIndex < 0)
        {
            return null;
        }

        var name = afterSeparator[..pipeIndex];

        return name.Length == 0 ? null : StripGenericArity(name);
    }

    private static string StripGenericArity(string name)
    {
        var tickIndex = name.LastIndexOf('`');

        return tickIndex < 0 ? name : name[..tickIndex];
    }
}
