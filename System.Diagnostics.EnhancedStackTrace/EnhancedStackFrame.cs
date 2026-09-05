using System.Diagnostics.EnhancedStackTrace.Formatting;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics.EnhancedStackTrace;

/// <summary>
/// One frame, kept as data rather than as a formatted line, so callers can filter and re-format
/// instead of parsing a string back apart.
/// </summary>
public sealed class EnhancedStackFrame
{
    private EnhancedStackFrame(MethodBase method, StackFrame frame)
    {
        Method = method;

        var compilerName = CompilerName.Parse(method.Name);
        var declaringType = ResolveDeclaringType(method);

        DeclaringTypeName = declaringType is null ? string.Empty : TypeAlias.Of(declaringType);
        Kind = ResolveKind(method, compilerName, declaringType);
        MethodName = ResolveMethodName(method, compilerName);
        OwnerMethodName = compilerName.Owner;

        var signature = ResolveSignature(method, declaringType, MethodName);

        ReturnTypeName = signature is MethodInfo methodInfo ? TypeAlias.Of(methodInfo.ReturnType) : string.Empty;
        Parameters = [.. signature.GetParameters().Select(Describe)];
        GenericArgumentNames = ResolveGenericArguments(signature);
        FileName = frame.GetFileName();
        LineNumber = frame.GetFileLineNumber();
    }

    public MethodBase Method { get; }

    public MethodKind Kind { get; }

    /// <summary>The name as written in source, with the compiler's rewriting undone.</summary>
    public string MethodName { get; }

    /// <summary>The method a lambda or local function was declared inside, when there is one.</summary>
    public string? OwnerMethodName { get; }

    public string DeclaringTypeName { get; }

    /// <summary>Empty for a constructor, which has no return type.</summary>
    public string ReturnTypeName { get; }

    public IReadOnlyList<StackFrameParameter> Parameters { get; }

    public IReadOnlyList<string> GenericArgumentNames { get; }

    /// <summary>Null unless the assembly was built with debug symbols available.</summary>
    public string? FileName { get; }

    /// <summary>Zero when no line information is available.</summary>
    public int LineNumber { get; }

    public bool HasSource
        => FileName is not null && LineNumber > 0;

    internal static EnhancedStackFrame? TryCreate(StackFrame frame)
    {
        var method = frame.GetMethod();

        return method is null || IsHidden(method)
            ? null
            : new EnhancedStackFrame(method, frame);
    }

    /// <summary>
    /// Async and iterator plumbing, and anything the runtime marks hidden, is noise between the
    /// call site and the throw.
    /// </summary>
    private static bool IsHidden(MethodBase method)
    {
        if (method.GetCustomAttribute<StackTraceHiddenAttribute>() is not null)
        {
            return true;
        }

        var declaringType = method.DeclaringType;

        if (declaringType is null)
        {
            return false;
        }

        if (declaringType.GetCustomAttribute<StackTraceHiddenAttribute>() is not null)
        {
            return true;
        }

        return declaringType.Namespace is "System.Runtime.CompilerServices" or "System.Runtime.ExceptionServices";
    }

    /// <summary>A state machine or closure is nested in the type that declared the original method.</summary>
    private static Type? ResolveDeclaringType(MethodBase method)
    {
        var declaringType = method.DeclaringType;

        while (declaringType is not null
            && declaringType.GetCustomAttribute<CompilerGeneratedAttribute>() is not null
            && declaringType.DeclaringType is not null)
        {
            declaringType = declaringType.DeclaringType;
        }

        return declaringType;
    }

    private static MethodKind ResolveKind(MethodBase method, CompilerName compilerName, Type? declaringType)
    {
        if (method is ConstructorInfo)
        {
            return MethodKind.Constructor;
        }

        if (IsStateMachine(method.DeclaringType))
        {
            return MethodKind.AsyncOrIterator;
        }

        if (compilerName.Kind is not MethodKind.Method)
        {
            return compilerName.Kind;
        }

        return AccessorKind(method, declaringType);
    }

    private static MethodKind AccessorKind(MethodBase method, Type? declaringType)
    {
        if (!method.IsSpecialName || declaringType is null)
        {
            return MethodKind.Method;
        }

        var property = Array.Find(
            declaringType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            candidate => candidate.GetMethod == method || candidate.SetMethod == method
        );

        if (property is null)
        {
            return MethodKind.Method;
        }

        if (property.GetIndexParameters().Length > 0)
        {
            return MethodKind.Indexer;
        }

        return property.GetMethod == method ? MethodKind.PropertyGetter : MethodKind.PropertySetter;
    }

    private string ResolveMethodName(MethodBase method, CompilerName compilerName)
    {
        // MoveNext carries no name; the state machine type does, as <Original>d__N.
        if (IsStateMachine(method.DeclaringType))
        {
            return CompilerName.Parse(method.DeclaringType!.Name).Name;
        }

        // An accessor is named get_X or set_X; the property is X.
        if (Kind is MethodKind.PropertyGetter or MethodKind.PropertySetter)
        {
            return compilerName.Name[4..];
        }

        return compilerName.Name;
    }

    /// <summary>
    /// A state machine's MoveNext returns void and takes nothing, which says nothing about the
    /// method it came from. Recover that method by name when it is unambiguous.
    /// </summary>
    private static MethodBase ResolveSignature(MethodBase method, Type? declaringType, string methodName)
    {
        if (!IsStateMachine(method.DeclaringType) || declaringType is null)
        {
            return method;
        }

        var candidates = declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(candidate => candidate.Name == methodName)
            .ToArray();

        return candidates.Length == 1 ? candidates[0] : method;
    }

    private static bool IsStateMachine(Type? type)
        => type is not null
            && type.GetCustomAttribute<CompilerGeneratedAttribute>() is not null
            && type.Name.Contains(">d__", StringComparison.Ordinal);

    private static IReadOnlyList<string> ResolveGenericArguments(MethodBase method)
        => method.IsGenericMethod
            ? [.. method.GetGenericArguments().Select(TypeAlias.Of)]
            : [];

    private static StackFrameParameter Describe(ParameterInfo parameter)
    {
        var modifier = parameter switch
        {
            { IsOut: true } => "out",
            { ParameterType.IsByRef: true, IsIn: true } => "in",
            { ParameterType.IsByRef: true } => "ref",
            _ => null
        };

        if (modifier is null && parameter.GetCustomAttribute<ParamArrayAttribute>() is not null)
        {
            modifier = "params";
        }

        return new StackFrameParameter(
            TypeAlias.Of(parameter.ParameterType),
            parameter.Name,
            modifier,
            DefaultValueOf(parameter)
        );
    }

    private static string? DefaultValueOf(ParameterInfo parameter)
    {
        if (!parameter.HasDefaultValue)
        {
            return null;
        }

        return parameter.DefaultValue switch
        {
            null => "null",
            string text => $"\"{text}\"",
            bool flag => flag ? "true" : "false",
            var value => value.ToString()
        };
    }

    public override string ToString()
    {
        var text = new StringBuilder();

        if (ReturnTypeName.Length > 0)
        {
            text.Append(ReturnTypeName).Append(' ');
        }

        text.Append(DeclaringTypeName);

        if (Kind is MethodKind.Indexer)
        {
            return text
                .Append('[')
                .AppendJoin(", ", Parameters)
                .Append(']')
                .Append(Source())
                .ToString();
        }

        if (Kind is MethodKind.Constructor)
        {
            return text
                .Insert(text.Length - DeclaringTypeName.Length, "new ")
                .Append('(')
                .AppendJoin(", ", Parameters)
                .Append(')')
                .Append(Source())
                .ToString();
        }

        if (Kind is MethodKind.PropertyGetter or MethodKind.PropertySetter)
        {
            var accessor = Kind is MethodKind.PropertyGetter ? "get" : "set";

            return text
                .Append('.')
                .Append(MethodName)
                .Append('.')
                .Append(accessor)
                .Append(Source())
                .ToString();
        }

        text.Append('.').Append(MethodName);

        if (OwnerMethodName is not null)
        {
            text.Insert(text.Length - MethodName.Length, $"{OwnerMethodName}()+");
        }
        else if (Kind is MethodKind.Lambda)
        {
            text.Append("()+lambda");

            return text.Append(Source()).ToString();
        }

        if (GenericArgumentNames.Count > 0)
        {
            text.Append('<').AppendJoin(", ", GenericArgumentNames).Append('>');
        }

        return text
            .Append('(')
            .AppendJoin(", ", Parameters)
            .Append(')')
            .Append(Source())
            .ToString();
    }

    private string Source()
        => HasSource ? $" in {FileName}:line {LineNumber}" : string.Empty;
}
