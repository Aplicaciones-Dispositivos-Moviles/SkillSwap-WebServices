// GetVerifierProfileByUserIdQuery.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

/// <summary>
///     Get the verifier profile of a user
/// </summary>
/// <param name="UserId">The user</param>
public record GetVerifierProfileByUserIdQuery(int UserId);