using System.Collections.Concurrent;

namespace System.Diagnostics.EnhancedStackTrace.Formatting;

/// <summary>Type names as they are written in C#: <c>int</c>, <c>List&lt;string&gt;</c>, <c>int?[]</c>.</summary>
internal static class TypeAlias
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(void)] = "void",
        [typeof(object)] = "object",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(string)] = "string",
        [typeof(nint)] = "nint",
        [typeof(nuint)] = "nuint"
    };

    public static string Of(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Cache.GetOrAdd(type, Build);
    }

    private static string Build(Type type)
    {
        if (Keywords.TryGetValue(type, out var keyword))
        {
            return keyword;
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return Of(underlying) + "?";
        }

        if (type.IsArray)
        {
            var commas = new string(',', type.GetArrayRank() - 1);

            return $"{Of(type.GetElementType()!)}[{commas}]";
        }

        if (type.IsByRef)
        {
            return Of(type.GetElementType()!);
        }

        return type.IsGenericType
            ? Qualify(type, BuildGeneric(type))
            : Qualify(type, type.Name);
    }

    /// <summary>
    /// A nested type reads as Outer.Inner; reflection alone would only give Inner. A generic
    /// parameter is excluded: its declaring type is the very type being built, so following it
    /// would never terminate.
    /// </summary>
    private static string Qualify(Type type, string name)
        => type is { IsNested: true, IsGenericParameter: false, DeclaringType: { } declaringType }
            ? $"{Of(declaringType)}.{name}"
            : name;

    private static string BuildGeneric(Type type)
    {
        var arguments = type.GetGenericArguments();

        // A tuple reads as (int, string), not as ValueTuple<int, string>.
        if (IsTuple(type))
        {
            return $"({string.Join(", ", arguments.Select(Of))})";
        }

        var name = type.Name;
        var tickIndex = name.LastIndexOf('`');

        if (tickIndex >= 0)
        {
            name = name[..tickIndex];
        }

        return $"{name}<{string.Join(", ", arguments.Select(Of))}>";
    }

    private static bool IsTuple(Type type)
        => type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) is true
            || type.FullName?.StartsWith("System.Tuple`", StringComparison.Ordinal) is true;
}
