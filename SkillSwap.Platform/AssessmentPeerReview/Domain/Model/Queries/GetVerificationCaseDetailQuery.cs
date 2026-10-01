namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

/// <summary>
///     Get a verification case with the questions the student answered incorrectly
/// </summary>
/// <param name="CaseId">The case</param>
public record GetVerificationCaseDetailQuery(int CaseId);