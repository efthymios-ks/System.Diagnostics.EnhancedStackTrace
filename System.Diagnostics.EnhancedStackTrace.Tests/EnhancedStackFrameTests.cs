using System.Diagnostics.EnhancedStackTrace.Tests.Shared;
using Xunit;

namespace System.Diagnostics.EnhancedStackTrace.Tests;

public class EnhancedStackFrameTests
{
    [Fact]
    public void MethodName_WhenTheFrameIsAPlainMethod_ShouldBeTheSourceName()
        => Assert.Equal(nameof(Throwers.FromMethod), FirstFrame(Throwers.FromMethod).MethodName);

    [Fact]
    public void Kind_WhenTheFrameIsAPlainMethod_ShouldBeMethod()
        => Assert.Equal(MethodKind.Method, FirstFrame(Throwers.FromMethod).Kind);

    [Fact]
    public void DeclaringTypeName_ShouldBeTheDeclaringType()
        => Assert.Equal(nameof(Throwers), FirstFrame(Throwers.FromMethod).DeclaringTypeName);

    [Fact]
    public void ReturnTypeName_ShouldUseTheCSharpKeyword()
        => Assert.Equal("void", FirstFrame(Throwers.FromMethod).ReturnTypeName);

    [Fact]
    public void MethodName_WhenTheFrameIsALambda_ShouldBeTheEnclosingMethod()
    {
        // Arrange & Act
        var frame = FirstFrame(Throwers.FromLambda);

        // Assert
        Assert.Equal(MethodKind.Lambda, frame.Kind);
        Assert.Equal(nameof(Throwers.FromLambda), frame.MethodName);
    }

    [Fact]
    public void ToString_WhenTheFrameIsALambda_ShouldTellItApartFromTheMethodAroundIt()
    {
        // Arrange & Act
        var trace = EnhancedStackTrace.Create(Throwers.Catch(Throwers.FromLambda));

        // Assert
        Assert.Equal($"void {nameof(Throwers)}.{nameof(Throwers.FromLambda)}()+lambda", Strip(trace[0]));
        Assert.Equal($"void {nameof(Throwers)}.{nameof(Throwers.FromLambda)}()", Strip(trace[1]));
    }

    [Fact]
    public async Task ToString_WhenTheFrameIsAsync_ShouldShowTheOriginalSignature()
    {
        // Arrange
        var exception = await Throwers.CatchAsync(Throwers.FromAsyncMethodAsync);
        var frame = EnhancedStackTrace.Create(exception)[0];

        // Act
        // MoveNext returns void and takes nothing; the method it was rewritten from does not.
        Assert.Equal("Task", frame.ReturnTypeName);
        Assert.Equal($"Task {nameof(Throwers)}.{nameof(Throwers.FromAsyncMethodAsync)}()", Strip(frame));
    }

    [Fact]
    public async Task Parameters_WhenTheFrameIsAsync_ShouldComeFromTheOriginalMethod()
    {
        // Arrange & Act
        var exception = await Throwers.CatchAsync(Throwers.FromAsyncMethodAsync);
        var caller = EnhancedStackTrace.Create(exception)[1];

        // Assert
        Assert.Equal("Func<Task> action", caller.Parameters.Single().ToString());
    }

    private static string Strip(EnhancedStackFrame frame)
    {
        var text = frame.ToString();
        var sourceIndex = text.IndexOf(" in ", StringComparison.Ordinal);

        return sourceIndex < 0 ? text : text[..sourceIndex];
    }

    [Fact]
    public void MethodName_WhenTheFrameIsALocalFunction_ShouldBeItsOwnName()
    {
        // Arrange & Act
        var frame = FirstFrame(Throwers.FromLocalFunction);

        // Assert
        Assert.Equal(MethodKind.LocalFunction, frame.Kind);
        Assert.Equal("Helper", frame.MethodName);
        Assert.Equal(nameof(Throwers.FromLocalFunction), frame.OwnerMethodName);
    }

    [Fact]
    public async Task MethodName_WhenTheFrameIsAnAsyncMethod_ShouldBeTheSourceName()
    {
        // Arrange & Act
        var exception = await Throwers.CatchAsync(Throwers.FromAsyncMethodAsync);
        var frame = EnhancedStackTrace.Create(exception)[0];

        // Assert
        Assert.Equal(MethodKind.AsyncOrIterator, frame.Kind);
        Assert.Equal(nameof(Throwers.FromAsyncMethodAsync), frame.MethodName);
    }

    [Fact]
    public async Task Create_WhenTheFrameIsAnAsyncMethod_ShouldNotReportTheStateMachineType()
    {
        // Arrange & Act
        var exception = await Throwers.CatchAsync(Throwers.FromAsyncMethodAsync);
        var frame = EnhancedStackTrace.Create(exception)[0];

        // Assert
        Assert.Equal(nameof(Throwers), frame.DeclaringTypeName);
        Assert.DoesNotContain("d__", frame.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_WhenTheFrameIsAnAsyncMethod_ShouldDropTheRuntimePlumbing()
    {
        // Arrange & Act
        var exception = await Throwers.CatchAsync(Throwers.FromAsyncMethodAsync);
        var text = EnhancedStackTrace.Create(exception).ToString();

        // Assert
        Assert.DoesNotContain("System.Runtime.CompilerServices", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Kind_WhenTheFrameIsAConstructor_ShouldBeConstructor()
    {
        // Arrange & Act
        var frame = FirstFrame(() => _ = new Widget());

        // Assert
        Assert.Equal(MethodKind.Constructor, frame.Kind);
        Assert.Equal(nameof(Widget), frame.DeclaringTypeName);
    }

    [Fact]
    public void ToString_WhenTheFrameIsAConstructor_ShouldReadAsANewExpression()
        => Assert.StartsWith("new Widget()", FirstFrame(() => _ = new Widget()).ToString());

    [Fact]
    public void Kind_WhenTheFrameIsAPropertyGetter_ShouldBePropertyGetter()
        => Assert.Equal(MethodKind.PropertyGetter, FirstFrame(() => _ = new Widget(1).Label).Kind);

    [Fact]
    public void MethodName_WhenTheFrameIsAnAccessor_ShouldBeThePropertyName()
    {
        // Act & Assert
        Assert.Equal(nameof(Widget.Label), FirstFrame(() => _ = new Widget(1).Label).MethodName);
        Assert.Equal(nameof(Meter.Reading), FirstFrame(() => new Meter().Reading = 5).MethodName);
    }

    [Fact]
    public void ToString_WhenTheFrameIsAnAccessor_ShouldNameThePropertyAndTheAccessor()
    {
        // Act & Assert
        Assert.StartsWith("string Widget.Label.get", FirstFrame(() => _ = new Widget(1).Label).ToString());
        Assert.StartsWith("void Meter.Reading.set", FirstFrame(() => new Meter().Reading = 5).ToString());
    }

    [Fact]
    public void Kind_WhenTheFrameIsAnIndexer_ShouldBeIndexer()
        => Assert.Equal(MethodKind.Indexer, FirstFrame(() => _ = new Widget(1)[0]).Kind);

    [Fact]
    public void ToString_WhenTheFrameIsAnIndexer_ShouldUseSquareBrackets()
    {
        // Arrange & Act
        var text = FirstFrame(() => _ = new Widget(1)[0]).ToString();

        // Assert
        Assert.Contains("Widget[int index]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Parameters_ShouldUseCSharpTypeNames()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromParameters(1, "x", out _));

        // Assert
        Assert.Equal(["int count", "string name", "out bool flag"], frame.Parameters.Select(p => p.ToString()));
    }

    [Fact]
    public void Parameters_WhenTheMethodTakesNone_ShouldBeEmpty()
        => Assert.Empty(FirstFrame(Throwers.FromMethod).Parameters);

    [Fact]
    public void GenericArgumentNames_WhenTheMethodIsGeneric_ShouldReportTheParameterNames()
    {
        // Arrange & Act
        // A stack frame carries the generic definition, so the runtime never tells us it was int.
        var frame = FirstFrame(() => Throwers.FromGenericMethod(7));

        // Assert
        Assert.Equal(["TValue"], frame.GenericArgumentNames);
        Assert.Contains("<TValue>", frame.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void DeclaringTypeName_WhenTheTypeIsGeneric_ShouldRenderItInAngleBrackets()
    {
        // Arrange & Act
        var frame = FirstFrame(() => new Box<string>().Open());

        // Assert
        Assert.Equal("Box<TItem>", frame.DeclaringTypeName);
    }

    [Fact]
    public void Method_ShouldExposeTheUnderlyingMethodBase()
        => Assert.Equal(nameof(Throwers.FromMethod), FirstFrame(Throwers.FromMethod).Method.Name);

    [Fact]
    public void HasSource_WhenTheLineIsKnown_ShouldIncludeItInToString()
    {
        // Arrange
        var frame = FirstFrame(Throwers.FromMethod);

        // Act
        if (!frame.HasSource)
        {
            return;
        }

        // Assert
        Assert.Contains($":line {frame.LineNumber}", frame.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_ShouldReadAsASignature()
    {
        // Arrange & Act
        var text = FirstFrame(Throwers.FromMethod).ToString();

        // Assert
        Assert.StartsWith($"void {nameof(Throwers)}.{nameof(Throwers.FromMethod)}()", text);
    }

    [Fact]
    public void MethodName_WhenTheFrameIsAnIterator_ShouldBeTheSourceName()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromIterator().ToArray());

        // Assert
        Assert.Equal(MethodKind.AsyncOrIterator, frame.Kind);
        Assert.Equal(nameof(Throwers.FromIterator), frame.MethodName);
        Assert.Equal(nameof(Throwers), frame.DeclaringTypeName);
    }

    [Fact]
    public void Parameters_WhenTheyHaveDefaults_ShouldShowTheDefaults()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromDefaults());

        // Assert
        Assert.Equal(
            ["int count = 5", "string name = \"x\"", "bool flag = true"],
            frame.Parameters.Select(parameter => parameter.ToString())
        );
    }

    [Fact]
    public void Parameters_WhenTheMethodTakesParams_ShouldShowTheModifier()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromParams(1, 2));

        // Assert
        Assert.Equal("params int[] values", frame.Parameters.Single().ToString());
    }

    [Fact]
    public void Parameters_WhenTheTypeIsATuple_ShouldRenderItInParentheses()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromTuple((1, "x")));

        // Assert
        Assert.Equal("(int, string) pair", frame.Parameters.Single().ToString());
    }

    [Fact]
    public void Parameters_WhenTheTypeIsNested_ShouldQualifyItWithTheOuterType()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromNestedType(new Outer.Inner()));

        // Assert
        Assert.Equal("Outer.Inner inner", frame.Parameters.Single().ToString());
    }

    private static EnhancedStackFrame FirstFrame(Action action)
        => EnhancedStackTrace.Create(Throwers.Catch(action))[0];
}
