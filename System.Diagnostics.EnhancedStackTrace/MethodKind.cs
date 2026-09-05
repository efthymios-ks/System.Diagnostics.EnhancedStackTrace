namespace System.Diagnostics.EnhancedStackTrace;

public enum MethodKind
{
    Method = 0,
    Constructor = 1,
    Lambda = 2,
    LocalFunction = 3,

    /// <summary>An async method or an iterator, both of which the compiler turns into a state machine.</summary>
    AsyncOrIterator = 4,

    PropertyGetter = 5,
    PropertySetter = 6,
    Indexer = 7
}
