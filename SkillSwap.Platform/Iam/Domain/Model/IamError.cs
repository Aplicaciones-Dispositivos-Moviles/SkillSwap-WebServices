namespace SkillSwap.Platform.Iam.Domain.Model;

public enum IamError
{
    None,
    InvalidCredentials,
    UserBanned,
    UsernameAlreadyTaken,
    EmailAlreadyTaken,
    InvalidInstitutionalEmail,
    InvalidUsername,
    WeakPassword,
    UserNotFound,
    NotProfileOwner,
    BioTooLong,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}