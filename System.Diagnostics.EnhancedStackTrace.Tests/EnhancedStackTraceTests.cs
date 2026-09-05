using System.Diagnostics.EnhancedStackTrace.Tests.Shared;
using Xunit;

namespace System.Diagnostics.EnhancedStackTrace.Tests;

public class EnhancedStackTraceTests
{
    [Fact]
    public void Create_WhenTheExceptionIsNull_ShouldThrowArgumentNull()
        => Assert.Throws<ArgumentNullException>(() => EnhancedStackTrace.Create((Exception)null!));

    [Fact]
    public void Create_WhenTheStackTraceIsNull_ShouldThrowArgumentNull()
        => Assert.Throws<ArgumentNullException>(() => EnhancedStackTrace.Create((StackTrace)null!));

    [Fact]
    public void Describe_WhenTheExceptionIsNull_ShouldThrowArgumentNull()
        => Assert.Throws<ArgumentNullException>(() => EnhancedStackTrace.Describe(null!));

    [Fact]
    public void Create_WhenTheExceptionWasThrown_ShouldReportTheThrowingFrameFirst()
    {
        // Arrange & Act
        var trace = EnhancedStackTrace.Create(Throwers.Catch(Throwers.FromMethod));

        // Assert
        Assert.Equal(nameof(Throwers.FromMethod), trace[0].MethodName);
    }

    [Fact]
    public void Create_WhenTheExceptionWasNeverThrown_ShouldBeEmpty()
        => Assert.Empty(EnhancedStackTrace.Create(new InvalidOperationException("never thrown")));

    [Fact]
    public void Create_WhenTheCallIsNested_ShouldReportTheFramesInnermostFirst()
    {
        // Arrange & Act
        var trace = EnhancedStackTrace.Create(Throwers.Catch(Throwers.FromNestedCall));

        // Assert
        Assert.Equal(nameof(Throwers.FromMethod), trace[0].MethodName);
        Assert.Equal(nameof(Throwers.FromNestedCall), trace[1].MethodName);
    }

    [Fact]
    public void Current_ShouldReportTheCallingMethodFirst()
    {
        // Arrange & Act
        var trace = EnhancedStackTrace.Current();

        // Assert
        Assert.Equal(nameof(Current_ShouldReportTheCallingMethodFirst), trace[0].MethodName);
    }

    [Fact]
    public void Count_ShouldMatchTheNumberOfFrames()
    {
        // Arrange & Act
        var trace = EnhancedStackTrace.Create(Throwers.Catch(Throwers.FromMethod));

        // Assert
        Assert.Equal(trace.Count, trace.Count());
    }

    [Fact]
    public void Indexer_ShouldReturnTheSameFrameAsEnumeration()
    {
        // Arrange & Act
        var trace = EnhancedStackTrace.Create(Throwers.Catch(Throwers.FromMethod));

        // Assert
        Assert.Same(trace.First(), trace[0]);
    }

    [Fact]
    public void ToString_ShouldPrefixEveryFrameWithAt()
    {
        // Arrange & Act
        var text = EnhancedStackTrace.Create(Throwers.Catch(Throwers.FromMethod)).ToString();

        // Assert
        Assert.StartsWith("   at ", text);
        Assert.Contains(nameof(Throwers.FromMethod), text);
    }

    [Fact]
    public void ToString_WhenThereAreNoFrames_ShouldBeEmpty()
        => Assert.Equal(string.Empty, EnhancedStackTrace.Create(new Exception()).ToString());

    [Fact]
    public void Describe_ShouldStartWithTheExceptionTypeAndMessage()
    {
        // Arrange & Act
        var text = EnhancedStackTrace.Describe(Throwers.Catch(Throwers.FromMethod));

        // Assert
        Assert.StartsWith($"{nameof(InvalidOperationException)}: method", text);
    }

    [Fact]
    public void Describe_WhenThereIsAnInnerException_ShouldIncludeItToo()
    {
        // Arrange
        var inner = Throwers.Catch(Throwers.FromMethod);
        var outer = new InvalidOperationException("outer", inner);

        // Act
        var text = EnhancedStackTrace.Describe(outer);

        // Assert
        Assert.Contains("outer", text);
        Assert.Contains("inner exception", text);
        Assert.Contains(nameof(Throwers.FromMethod), text);
    }

    [Fact]
    public void Describe_WhenTheExceptionAggregatesSeveral_ShouldIncludeEveryOne()
    {
        // Arrange
        var aggregate = new AggregateException(
            new InvalidOperationException("first"),
            new InvalidOperationException("second")
        );

        // Act
        var text = EnhancedStackTrace.Describe(aggregate);

        // Assert
        Assert.Contains("first", text);
        Assert.Contains("second", text);
    }

    [Fact]
    public void GetEnhancedStackTrace_ShouldReturnTheSameFramesAsCreate()
    {
        // Arrange & Act
        var exception = Throwers.Catch(Throwers.FromMethod);

        // Assert
        Assert.Equal(
            EnhancedStackTrace.Create(exception).Count,
            exception.GetEnhancedStackTrace().Count
        );
    }

    [Fact]
    public void ToEnhancedString_ShouldMatchDescribe()
    {
        // Arrange & Act
        var exception = Throwers.Catch(Throwers.FromMethod);

        // Assert
        Assert.Equal(EnhancedStackTrace.Describe(exception), exception.ToEnhancedString());
    }

    [Fact]
    public void GetEnhancedStackTrace_ShouldLeaveTheExceptionUntouched()
    {
        // Arrange
        var exception = Throwers.Catch(Throwers.FromMethod);
        var before = exception.StackTrace;

        // Act
        exception.GetEnhancedStackTrace();

        // Assert
        Assert.Equal(before, exception.StackTrace);
    }
}
