namespace SkillSwap.Platform.Reputation.Domain.Model;

public enum ReputationError
{
    None,
    ReputationNotFound,
    NotReputationOwner,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}