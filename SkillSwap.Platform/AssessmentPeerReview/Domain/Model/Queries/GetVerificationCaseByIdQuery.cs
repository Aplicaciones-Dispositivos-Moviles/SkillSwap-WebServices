// GetVerificationCaseByIdQuery.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

/// <summary>
///     Get a verification case by its id
/// </summary>
/// <param name="CaseId">The case</param>
public record GetVerificationCaseByIdQuery(int CaseId);