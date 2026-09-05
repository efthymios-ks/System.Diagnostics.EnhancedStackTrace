using System.Reflection;

namespace System.Diagnostics.EnhancedStackTrace;

public static class ExceptionExtensions
{
    /// <summary>
    /// The runtime returns this field from <see cref="Exception.StackTrace"/> when it is set, which
    /// is the only way to reach code that already calls ToString on the exception. Private, so a
    /// runtime that renames it leaves this null and <see cref="Enhance"/> becomes a no-op.
    /// </summary>
    private static readonly FieldInfo? StackTraceField =
        typeof(Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>The exception's stack trace, enhanced. The exception itself is not touched.</summary>
    public static EnhancedStackTrace GetEnhancedStackTrace(this Exception exception)
        => EnhancedStackTrace.Create(exception);

    /// <summary>The message and enhanced trace of this exception and every inner one.</summary>
    public static string ToEnhancedString(this Exception exception)
        => EnhancedStackTrace.Describe(exception);

    /// <summary>
    /// Rewrites the exception's own <see cref="Exception.StackTrace"/> in place, and every inner
    /// exception's, so loggers and handlers that were already calling ToString print the enhanced
    /// trace without being changed.
    /// </summary>
    /// <remarks>
    /// Safe to call more than once: the enhanced trace is rebuilt from the frames the runtime
    /// recorded, not from the string this replaced. Does nothing when the exception was never
    /// thrown, or on a runtime where the field is absent.
    /// </remarks>
    public static TException Enhance<TException>(this TException exception) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(exception);

        Enhance(exception, new HashSet<Exception>(ReferenceEqualityComparer.Instance));

        return exception;
    }

    public static bool CanEnhanceInPlace
        => StackTraceField is not null;

    private static void Enhance(Exception exception, HashSet<Exception> visited)
    {
        // An exception can appear twice in a graph, and an AggregateException can hold itself.
        if (!visited.Add(exception))
        {
            return;
        }

        Overwrite(exception);

        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                Enhance(inner, visited);
            }

            return;
        }

        if (exception.InnerException is { } innerException)
        {
            Enhance(innerException, visited);
        }
    }

    private static void Overwrite(Exception exception)
    {
        if (StackTraceField is null)
        {
            return;
        }

        var enhanced = EnhancedStackTrace.Create(exception).ToString();

        // An exception that was never thrown has no frames; blanking its trace would lose nothing
        // but would also say nothing.
        if (enhanced.Length > 0)
        {
            StackTraceField.SetValue(exception, enhanced);
        }
    }
}
