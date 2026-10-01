// GetAssessmentAttemptByIdQuery.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

/// <summary>
///     Get an assessment attempt by its id
/// </summary>
/// <param name="AttemptId">The attempt</param>
public record GetAssessmentAttemptByIdQuery(int AttemptId);