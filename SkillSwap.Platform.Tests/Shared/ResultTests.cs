using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.Tests.Shared;

public class ResultTests
{
    private enum SampleError
    {
        Conflict
    }

    [Fact]
    public void Success_HasNoErrorAndNoDetails()
    {
        var result = Result<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.Details);
    }

    [Fact]
    public void Failure_WithoutDetails_HasNoDetails()
    {
        var result = Result<string>.Failure(SampleError.Conflict, "message");

        Assert.True(result.IsFailure);
        Assert.Equal(SampleError.Conflict, result.Error);
        Assert.Equal("message", result.Message);
        Assert.Null(result.Details);
    }

    [Fact]
    public void Failure_WithDetails_ExposesThem()
    {
        var details = new Dictionary<string, object> { ["existingId"] = 12 };

        var result = Result<string>.Failure(SampleError.Conflict, "message", details);

        Assert.Equal(12, result.Details!["existingId"]);
    }

    [Fact]
    public void NonGenericFailure_StillWorks()
    {
        var result = Result.Failure(SampleError.Conflict, "message");

        Assert.True(result.IsFailure);
        Assert.Equal("message", result.Message);
    }
}