// GetVerificationCasesByVerifierQuery.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

/// <summary>
///     Get the cases assigned to a verifier
/// </summary>
/// <param name="VerifierUserId">The verifier's user id</param>
public record GetVerificationCasesByVerifierQuery(int VerifierUserId);