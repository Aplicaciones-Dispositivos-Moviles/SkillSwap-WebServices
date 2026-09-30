using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Domain;

public class UserTests
{
    [Fact]
    public void NewUser_StartsUnverifiedWithEmptyBioAndNoDeviceToken()
    {
        var user = TestData.NewUser();

        Assert.False(user.IsVerified);
        Assert.Equal(string.Empty, user.Bio);
        Assert.Null(user.DeviceToken);
    }

    [Fact]
    public void Verify_MarksUserAsVerified()
    {
        var user = TestData.NewUser();

        user.Verify();

        Assert.True(user.IsVerified);
    }

    [Fact]
    public void UpdateBio_TrimsAndStoresTheText()
    {
        var user = TestData.NewUser();

        user.UpdateBio("  Backend developer  ");

        Assert.Equal("Backend developer", user.Bio);
    }

    [Fact]
    public void UpdateBio_WithExactlyMaxLength_IsAccepted()
    {
        var user = TestData.NewUser();

        user.UpdateBio(new string('a', User.MaxBioLength));

        Assert.Equal(User.MaxBioLength, user.Bio.Length);
    }

    [Fact]
    public void UpdateBio_ExceedingMaxLength_ThrowsDomainException()
    {
        var user = TestData.NewUser();

        Assert.Throws<DomainException>(() => user.UpdateBio(new string('a', User.MaxBioLength + 1)));
    }

    [Fact]
    public void RegisterDeviceToken_StoresTheToken()
    {
        var user = TestData.NewUser();

        user.RegisterDeviceToken(" device-123 ");

        Assert.Equal(new DeviceToken("device-123"), user.DeviceToken);
    }

    [Fact]
    public void RegisterDeviceToken_WithBlankToken_ThrowsDomainException()
    {
        var user = TestData.NewUser();

        Assert.Throws<DomainException>(() => user.RegisterDeviceToken("  "));
    }
}