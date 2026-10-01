using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

/// <summary>
///     VerificationCase aggregate root
/// </summary>
/// <remarks>
///     Opened when an attempt is not approved. It is assigned to an enabled verifier, who resolves it
///     with a decision and the notes of the rubric. A student can add evidence while it is open.
/// </remarks>
public class VerificationCase
{
    public const int MaxEvidenceUrlLength = 500;
    public const int MaxRubricNotesLength = 2000;

    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected VerificationCase()
    {
        SkillTag = null!;
    }

    public VerificationCase(int attemptId, int studentId, int pathNodeId, string skillTag)
    {
        if (attemptId <= 0)
            throw new DomainException("The case must belong to a valid attempt.");
        if (studentId <= 0)
            throw new DomainException("The case must belong to a valid student.");
        if (pathNodeId <= 0)
            throw new DomainException("The case must belong to a valid path node.");
        if (string.IsNullOrWhiteSpace(skillTag))
            throw new DomainException("The skill tag cannot be empty.");

        AttemptId = attemptId;
        StudentId = studentId;
        PathNodeId = pathNodeId;
        SkillTag = skillTag.Trim();
        Status = CaseStatus.Pending;
        OpenedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int AttemptId { get; private set; }
    public int StudentId { get; private set; }

    /// <summary>
    ///     The user id of the assigned verifier; null while the case is pending.
    /// </summary>
    public int? VerifierUserId { get; private set; }

    public int PathNodeId { get; private set; }
    public string SkillTag { get; private set; }
    public CaseStatus Status { get; private set; }
    public ReviewDecision? Decision { get; private set; }
    public string? RubricNotes { get; private set; }
    public string? EvidenceUrl { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public bool IsOpen => Status != CaseStatus.Resolved;

    public bool IsAssignedTo(int userId)
    {
        return VerifierUserId == userId;
    }

    /// <summary>
    ///     Assigns the case to a verifier. A student can never review their own case.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the case is not pending or the verifier is not valid.</exception>
    public VerificationCase AssignVerifier(int verifierUserId)
    {
        if (Status != CaseStatus.Pending)
            throw new DomainException("Only a pending case can be assigned.");
        if (verifierUserId <= 0)
            throw new DomainException("The verifier must be a valid user.");
        if (verifierUserId == StudentId)
            throw new DomainException("A student cannot review their own case.");

        VerifierUserId = verifierUserId;
        Status = CaseStatus.Assigned;
        AssignedAt = DateTime.UtcNow;
        return this;
    }

    /// <summary>
    ///     Attaches the link to the student's repository or portfolio. A new link replaces the previous one.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the case is resolved or the link is not a valid http(s) URL.</exception>
    public VerificationCase AttachEvidence(string evidenceUrl)
    {
        if (Status == CaseStatus.Resolved)
            throw new DomainException("A resolved case does not accept evidence.");

        var url = evidenceUrl?.Trim() ?? string.Empty;
        if (url.Length == 0)
            throw new DomainException("The evidence link cannot be empty.");
        if (url.Length > MaxEvidenceUrlLength)
            throw new DomainException($"The evidence link cannot exceed {MaxEvidenceUrlLength} characters.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new DomainException("The evidence link must be a valid http or https URL.");

        EvidenceUrl = url;
        return this;
    }

    /// <summary>
    ///     Records the verifier's decision and the notes of the rubric.
    /// </summary>
    /// <exception cref="DomainException">
    ///     Thrown when the case is already resolved, has no assigned verifier, or the notes are empty or too long.
    /// </exception>
    public VerificationCase Resolve(ReviewDecision decision, string rubricNotes)
    {
        if (Status == CaseStatus.Resolved)
            throw new DomainException("The case is already resolved.");
        if (Status != CaseStatus.Assigned)
            throw new DomainException("A case needs an assigned verifier before it can be resolved.");
        if (!Enum.IsDefined(decision))
            throw new DomainException("The decision is not valid.");

        var notes = rubricNotes?.Trim() ?? string.Empty;
        if (notes.Length == 0)
            throw new DomainException("The rubric notes are required.");
        if (notes.Length > MaxRubricNotesLength)
            throw new DomainException($"The rubric notes cannot exceed {MaxRubricNotesLength} characters.");

        Decision = decision;
        RubricNotes = notes;
        Status = CaseStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        return this;
    }
}