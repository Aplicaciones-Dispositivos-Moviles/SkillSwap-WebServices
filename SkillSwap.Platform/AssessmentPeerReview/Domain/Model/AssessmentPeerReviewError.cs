namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model;

public enum AssessmentPeerReviewError
{
    None,
    InvalidAnswers,
    InvalidEvidenceUrl,
    InvalidSkillTag,
    RubricNotesRequired,
    BlueprintNotFound,
    AttemptNotFound,
    CaseNotFound,
    VerifierProfileNotFound,
    NotBlueprintOwner,
    NotAttemptOwner,
    NotCaseOwner,
    NotAssignedVerifier,
    NotAVerifier,
    BlueprintOutdated,
    AttemptAlreadySubmitted,
    NodeNotAvailable,
    OpenCaseAlreadyExists,
    CaseAlreadyResolved,
    CaseNotAssigned,
    SkillNotCompleted,
    VerifierSkillAlreadyEnabled,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}