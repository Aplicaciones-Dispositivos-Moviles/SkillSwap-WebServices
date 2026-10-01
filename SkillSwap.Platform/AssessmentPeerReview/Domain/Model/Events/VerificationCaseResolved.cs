// VerificationCaseResolved.cs
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;

/// <summary>
///     A verifier resolved a verification case, approving or rejecting it.
/// </summary>
public sealed record VerificationCaseResolved(
    int CaseId,
    int StudentId,
    int VerifierUserId,
    int PathNodeId,
    string SkillTag,
    ReviewDecision Decision) : IDomainEvent;