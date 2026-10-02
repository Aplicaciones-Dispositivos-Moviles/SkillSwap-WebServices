using SkillSwap.Platform.AssessmentPeerReview.Application.ACL;

namespace SkillSwap.Platform.Tests.Support;

public class FakeVerifierProfileContextFacade : IVerifierProfileContextFacade
{
    public List<(int VerifierUserId, double Rating)> Updates { get; } = [];

    /// <summary>
    ///     What <see cref="UpdateRatingAsync" /> reports: false simulates a user without a verifier profile.
    /// </summary>
    public bool Updated { get; set; } = true;

    public Exception? ExceptionToThrow { get; set; }

    public Task<bool> UpdateRatingAsync(int verifierUserId, double rating, CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        Updates.Add((verifierUserId, rating));
        return Task.FromResult(Updated);
    }
}