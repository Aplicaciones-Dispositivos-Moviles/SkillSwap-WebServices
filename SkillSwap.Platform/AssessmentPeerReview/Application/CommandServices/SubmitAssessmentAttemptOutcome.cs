using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;

/// <summary>
///     Result of submitting an attempt: the graded attempt and, when it was not approved, the
///     verification case that was opened.
/// </summary>
public sealed record SubmitAssessmentAttemptOutcome(AssessmentAttempt Attempt, VerificationCase? VerificationCase);