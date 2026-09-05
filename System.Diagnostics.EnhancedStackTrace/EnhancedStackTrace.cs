using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics.EnhancedStackTrace;

/// <summary>
/// A stack trace with the compiler's rewriting undone: real method names for lambdas, local
/// functions and async methods, C# type names, and no state-machine plumbing.
/// </summary>
public sealed class EnhancedStackTrace : IReadOnlyList<EnhancedStackFrame>
{
    private readonly EnhancedStackFrame[] _frames;

    private EnhancedStackTrace(EnhancedStackFrame[] frames)
        => _frames = frames;

    public int Count
        => _frames.Length;

    public EnhancedStackFrame this[int index]
        => _frames[index];

    public static EnhancedStackTrace Create(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return Create(new StackTrace(exception, fNeedFileInfo: true));
    }

    public static EnhancedStackTrace Create(StackTrace stackTrace)
        => Create(stackTrace, skipOwnFrames: false);

    private static EnhancedStackTrace Create(StackTrace stackTrace, bool skipOwnFrames)
    {
        ArgumentNullException.ThrowIfNull(stackTrace);

        var frames = stackTrace
            .GetFrames()
            .Select(EnhancedStackFrame.TryCreate)
            .OfType<EnhancedStackFrame>();

        if (skipOwnFrames)
        {
            frames = frames.SkipWhile(frame => frame.Method.DeclaringType == typeof(EnhancedStackTrace));
        }

        return new EnhancedStackTrace([.. frames]);
    }

    /// <summary>
    /// The caller's own stack. This type's own frames are dropped by declaring type rather than by
    /// a frame count, which inlining and the runtime's own invocation frames make unreliable.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static EnhancedStackTrace Current()
        => Create(new StackTrace(fNeedFileInfo: true), skipOwnFrames: true);

    /// <summary>The exception's message and enhanced trace, plus every inner exception's.</summary>
    public static string Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var text = new StringBuilder();

        Describe(exception, text);

        return text.ToString();
    }

    public IEnumerator<EnhancedStackFrame> GetEnumerator()
        => ((IEnumerable<EnhancedStackFrame>)_frames).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _frames.GetEnumerator();

    public override string ToString()
        => string.Join(Environment.NewLine, _frames.Select(frame => $"   at {frame}"));

    private static void Describe(Exception exception, StringBuilder text)
    {
        text.Append(exception.GetType().Name).Append(": ").AppendLine(exception.Message);

        var trace = Create(exception).ToString();

        if (trace.Length > 0)
        {
            text.AppendLine(trace);
        }

        foreach (var inner in InnerExceptions(exception))
        {
            text.AppendLine(" ---> inner exception:");
            Describe(inner, text);
        }
    }

    /// <summary>An AggregateException hides several; only reporting InnerException would lose the rest.</summary>
    private static IEnumerable<Exception> InnerExceptions(Exception exception)
        => exception is AggregateException aggregate
            ? aggregate.InnerExceptions
            : exception.InnerException is { } inner ? [inner] : [];
}
