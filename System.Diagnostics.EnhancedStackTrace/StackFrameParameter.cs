namespace System.Diagnostics.EnhancedStackTrace;

/// <summary>One parameter, already written the way it appears in source.</summary>
public sealed record StackFrameParameter(
    string TypeName,
    string? Name,
    string? Modifier,
    string? DefaultValue
)
{
    public override string ToString()
    {
        var text = Modifier is null ? TypeName : $"{Modifier} {TypeName}";

        if (Name is not null)
        {
            text += $" {Name}";
        }

        return DefaultValue is null ? text : $"{text} = {DefaultValue}";
    }
}
