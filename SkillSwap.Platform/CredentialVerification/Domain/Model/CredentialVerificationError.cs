namespace SkillSwap.Platform.CredentialVerification.Domain.Model;

public enum CredentialVerificationError
{
    None,
    FileRequired,
    InvalidFileType,
    FileTooLarge,
    DuplicateFile,
    FieldTooLong,
    CertificateNotFound,
    NotCertificateOwner,
    InvalidStatusTransition,
    StorageError,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}