using System.Diagnostics.EnhancedStackTrace.Tests.Shared;
using Xunit;

namespace System.Diagnostics.EnhancedStackTrace.Tests;

/// <summary>The remaining shapes the formatter has to get right, one test each.</summary>
public class FormattingTests
{
    [Fact]
    public void Parameters_WhenTheyAreRefAndIn_ShouldShowBothModifiers()
    {
        // Arrange
        var counter = 0;
        var rate = 1.5d;

        // Act
        var frame = FirstFrame(() => Throwers.FromRefAndIn(ref counter, in rate));

        // Assert
        Assert.Equal(
            ["ref int counter", "in double rate"],
            frame.Parameters.Select(parameter => parameter.ToString())
        );
    }

    [Fact]
    public void Parameters_WhenTheTypeIsNullable_ShouldAppendAQuestionMark()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromNullable(1, DateTime.UtcNow));

        // Assert
        Assert.Equal(
            ["int? maybe", "DateTime? when"],
            frame.Parameters.Select(parameter => parameter.ToString())
        );
    }

    [Fact]
    public void Parameters_WhenTheTypeIsATupleClass_ShouldRenderItInParentheses()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromTupleClass(Tuple.Create(1, "x")));

        // Assert
        Assert.Equal("(int, string) pair", frame.Parameters.Single().ToString());
    }

    [Fact]
    public void Parameters_WhenTheTypeIsAMultiDimensionalArray_ShouldShowItsRank()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromMatrix(new int[1, 1], 0));

        // Assert
        Assert.Equal("int[,] grid", frame.Parameters[0].ToString());
    }

    [Fact]
    public void Parameters_WhenTheTypeIsNativeSized_ShouldUseTheCSharpKeyword()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromMatrix(new int[1, 1], 0));

        // Assert
        Assert.Equal("nint handle", frame.Parameters[1].ToString());
    }

    [Fact]
    public void Parameters_WhenTheTypeIsGeneric_ShouldDropTheArityTick()
    {
        // Arrange & Act
        var frame = FirstFrame(() => Throwers.FromGenericHolder(new Holder<int, string>()));

        // Assert
        Assert.Equal("Holder<int, string> holder", frame.Parameters.Single().ToString());
    }

    [Fact]
    public void Kind_WhenTheFrameIsAPropertySetter_ShouldBePropertySetter()
        => Assert.Equal(MethodKind.PropertySetter, FirstFrame(() => new Meter().Reading = 5).Kind);

    [Fact]
    public void Create_WhenAFrameIsMarkedStackTraceHidden_ShouldLeaveItOut()
    {
        // Arrange & Act
        var trace = EnhancedStackTrace.Create(Throwers.Catch(Throwers.FromHiddenWrapper));

        // Assert
        Assert.DoesNotContain(trace, frame => frame.MethodName == nameof(Throwers.Hidden));
        Assert.Contains(trace, frame => frame.MethodName == nameof(Throwers.FromHiddenWrapper));
    }

    [Fact]
    public void Kind_WhenTheFrameIsAStaticConstructor_ShouldBeConstructor()
    {
        // Arrange & Act
        var exception = Throwers.Catch(() => _ = FailingInitialiser.Value);
        var frame = EnhancedStackTrace.Create(exception.InnerException ?? exception)[0];

        // Assert
        Assert.Equal(MethodKind.Constructor, frame.Kind);
        Assert.Equal(nameof(FailingInitialiser), frame.DeclaringTypeName);
    }

    private static EnhancedStackFrame FirstFrame(Action action)
        => EnhancedStackTrace.Create(Throwers.Catch(action))[0];
}
