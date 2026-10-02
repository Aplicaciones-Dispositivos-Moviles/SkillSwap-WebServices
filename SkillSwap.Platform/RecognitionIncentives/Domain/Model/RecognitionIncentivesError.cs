namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model;

public enum RecognitionIncentivesError
{
    None,
    InvalidRedemptionItem,
    WalletNotFound,
    NotWalletOwner,
    InsufficientBalance,
    ConcurrentUpdate,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}