// AssessmentAttemptPassed.cs
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;

/// <summary>
///     A student passed the assessment of a node automatically.
/// </summary>
public sealed record AssessmentAttemptPassed(int AttemptId, int StudentId, int PathNodeId, string SkillTag)
    : IDomainEvent;