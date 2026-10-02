using SkillSwap.Platform.AssessmentPeerReview.Application.ACL;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Exceptions;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Application;

public class VerifierProfileContextFacadeTests
{
    private readonly VerifierProfileContextFacade _facade;
    private readonly FakeVerifierProfileRepository _profiles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    public VerifierProfileContextFacadeTests()
    {
        _facade = new VerifierProfileContextFacade(_profiles, _unitOfWork);
    }

    [Fact]
    public async Task UpdateRating_OfAVerifier_StoresItAndSaves()
    {
        var profile = new VerifierProfile(2, "http-basics");
        await _profiles.AddAsync(profile);

        var updated = await _facade.UpdateRatingAsync(2, 85, CancellationToken.None);

        Assert.True(updated);
        Assert.Equal(85, profile.Rating);
        Assert.Equal(1, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task UpdateRating_DoesNotTouchTheReviewCount()
    {
        var profile = new VerifierProfile(2, "http-basics").IncrementReviewCount().IncrementReviewCount();
        await _profiles.AddAsync(profile);

        await _facade.UpdateRatingAsync(2, 70, CancellationToken.None);

        Assert.Equal(2, profile.ReviewCount);
    }

    [Fact]
    public async Task UpdateRating_OnlyChangesTheProfileOfThatUser()
    {
        var target = new VerifierProfile(2, "http-basics");
        var other = new VerifierProfile(3, "http-basics");
        await _profiles.AddAsync(target);
        await _profiles.AddAsync(other);

        await _facade.UpdateRatingAsync(2, 60, CancellationToken.None);

        Assert.Equal(60, target.Rating);
        Assert.Equal(0, other.Rating);
    }

    [Fact]
    public async Task UpdateRating_ForAUserWithoutAProfile_ReturnsFalseAndSavesNothing()
    {
        var updated = await _facade.UpdateRatingAsync(99, 85, CancellationToken.None);

        Assert.False(updated);
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task UpdateRating_OfARevokedProfile_StillStoresIt()
    {
        var profile = new VerifierProfile(2, "http-basics").Revoke();
        await _profiles.AddAsync(profile);

        var updated = await _facade.UpdateRatingAsync(2, 40, CancellationToken.None);

        Assert.True(updated);
        Assert.Equal(40, profile.Rating);
    }

    [Fact]
    public async Task UpdateRating_WithAnInvalidRating_ThrowsAndSavesNothing()
    {
        await _profiles.AddAsync(new VerifierProfile(2, "http-basics"));

        await Assert.ThrowsAsync<DomainException>(() => _facade.UpdateRatingAsync(2, -1, CancellationToken.None));
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task UpdateRating_WhenPersistenceFails_Propagates()
    {
        await _profiles.AddAsync(new VerifierProfile(2, "http-basics"));
        _unitOfWork.ExceptionToThrow = new InvalidOperationException("database down");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.UpdateRatingAsync(2, 80, CancellationToken.None));
    }
}