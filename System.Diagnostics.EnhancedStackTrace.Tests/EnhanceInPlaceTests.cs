using System.Diagnostics.EnhancedStackTrace.Tests.Shared;
using System.Reflection;
using Xunit;

namespace System.Diagnostics.EnhancedStackTrace.Tests;

public class EnhanceInPlaceTests
{
    [Fact]
    public void Enhance_WhenTheExceptionIsNull_ShouldThrowArgumentNull()
        => Assert.Throws<ArgumentNullException>(() => ((Exception)null!).Enhance());

    [Fact]
    public void Exception_ShouldStillHaveThePrivateStackTraceField()
    {
        // Arrange & Act
        // Enhance writes to this field. A runtime that renames it makes Enhance a silent no-op, so
        // this test is what turns that into a visible failure.
        var field = typeof(Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(field);
        Assert.Equal(typeof(string), field.FieldType);
    }

    [Fact]
    public void CanEnhanceInPlace_OnThisRuntime_ShouldBeTrue()
        => Assert.True(ExceptionExtensions.CanEnhanceInPlace);

    [Fact]
    public void Enhance_ShouldReplaceTheStackTraceWithTheEnhancedOne()
    {
        // Arrange
        var exception = Throwers.Catch(Throwers.FromMethod);
        var expected = exception.GetEnhancedStackTrace().ToString();

        // Act
        exception.Enhance();

        // Assert
        Assert.Equal(expected, exception.StackTrace);
    }

    [Fact]
    public void Enhance_ShouldReturnTheSameExceptionSoItCanBeChained()
    {
        // Arrange & Act
        var exception = Throwers.Catch(Throwers.FromMethod);

        // Assert
        Assert.Same(exception, exception.Enhance());
    }

    [Fact]
    public void Enhance_ShouldKeepTheExceptionsOwnType()
    {
        // Arrange & Act
        var exception = (InvalidOperationException)Throwers.Catch(Throwers.FromMethod);

        // Assert
        Assert.IsType<InvalidOperationException>(exception.Enhance());
    }

    [Fact]
    public void Enhance_ShouldChangeWhatToStringPrints()
    {
        // Arrange
        var exception = Throwers.Catch(Throwers.FromLambda);
        var before = exception.ToString();

        // Act
        exception.Enhance();

        // Assert
        Assert.NotEqual(before, exception.ToString());
        Assert.Contains(nameof(Throwers.FromLambda), exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enhance_WhenTheFrameIsAsync_ShouldRemoveTheStateMachineFromToString()
    {
        // Arrange
        var exception = await Throwers.CatchAsync(Throwers.FromAsyncMethodAsync);

        // Act
        exception.Enhance();

        // Assert
        Assert.DoesNotContain("MoveNext", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains(nameof(Throwers.FromAsyncMethodAsync), exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Enhance_ShouldEnhanceTheInnerExceptionToo()
    {
        // Arrange
        var inner = Throwers.Catch(Throwers.FromMethod);
        var outer = new InvalidOperationException("outer", inner);

        // Act
        outer.Enhance();

        // Assert
        Assert.Equal(inner.GetEnhancedStackTrace().ToString(), inner.StackTrace);
    }

    [Fact]
    public void Enhance_ShouldEnhanceEveryExceptionInsideAnAggregate()
    {
        // Arrange
        var first = Throwers.Catch(Throwers.FromMethod);
        var second = Throwers.Catch(Throwers.FromLambda);
        var aggregate = new AggregateException(first, second);

        // Act
        aggregate.Enhance();

        // Assert
        Assert.Equal(first.GetEnhancedStackTrace().ToString(), first.StackTrace);
        Assert.Equal(second.GetEnhancedStackTrace().ToString(), second.StackTrace);
    }

    [Fact]
    public void Enhance_WhenCalledTwice_ShouldProduceTheSameTrace()
    {
        // Arrange
        var exception = Throwers.Catch(Throwers.FromMethod);

        exception.Enhance();
        var once = exception.StackTrace;

        // Act
        exception.Enhance();

        // Assert
        Assert.Equal(once, exception.StackTrace);
    }

    [Fact]
    public void Enhance_WhenTheExceptionWasNeverThrown_ShouldLeaveTheTraceAlone()
    {
        // Arrange
        var exception = new InvalidOperationException("never thrown");

        // Act
        exception.Enhance();

        // Assert
        Assert.Null(exception.StackTrace);
    }

    [Fact]
    public void Enhance_WhenTheGraphContainsTheSameExceptionTwice_ShouldNotRecurseForever()
    {
        // Arrange
        var shared = Throwers.Catch(Throwers.FromMethod);
        var aggregate = new AggregateException(shared, shared);

        // Act
        aggregate.Enhance();

        // Assert
        Assert.Equal(shared.GetEnhancedStackTrace().ToString(), shared.StackTrace);
    }

    [Fact]
    public void Enhance_WhenTheChainIsDeep_ShouldReachTheInnermostException()
    {
        // Arrange
        var innermost = Throwers.Catch(Throwers.FromMethod);
        var middle = new InvalidOperationException("middle", innermost);
        var outer = new InvalidOperationException("outer", middle);

        // Act
        outer.Enhance();

        // Assert
        Assert.Equal(innermost.GetEnhancedStackTrace().ToString(), innermost.StackTrace);
    }
}
