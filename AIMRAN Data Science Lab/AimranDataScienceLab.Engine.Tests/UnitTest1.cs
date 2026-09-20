using Xunit;

namespace AimranDataScienceLab.Engine.Tests;

public sealed class EngineResultTests
{
    [Fact]
    public void WhenOkThenSucceededIsTrue()
    {
        var result = EngineResult.Ok();
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void WhenOkThenErrorIsNone()
    {
        var result = EngineResult.Ok();
        Assert.Equal(EngineError.None, result.Error);
    }

    [Fact]
    public void WhenFailThenSucceededIsFalse()
    {
        var result = EngineResult.Fail(EngineErrorCode.ValidationFailed, "bad");
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void WhenFailThenErrorIsSet()
    {
        var result = EngineResult.Fail(EngineErrorCode.ValidationFailed, "bad");
        Assert.Equal(EngineErrorCode.ValidationFailed, result.Error.Code);
    }
}

public sealed class EngineResultOfTTests
{
    [Fact]
    public void WhenOkThenValueIsReturned()
    {
        var result = EngineResult<int>.Ok(42);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void WhenFailThenSucceededIsFalse()
    {
        var result = EngineResult<int>.Fail(EngineErrorCode.NotFound, "missing");
        Assert.False(result.Succeeded);
    }
}
