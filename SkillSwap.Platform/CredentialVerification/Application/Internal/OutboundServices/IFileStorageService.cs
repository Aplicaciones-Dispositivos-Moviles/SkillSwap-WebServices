namespace SkillSwap.Platform.CredentialVerification.Application.Internal.OutboundServices;

/// <summary>
///     Contract for storing the certificate files outside the database, decoupling the application
///     from the concrete provider. Files are private: they can only be read through a temporary URL.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    ///     Stores a file and returns the reference needed to retrieve or delete it later.
    /// </summary>
    /// <param name="content">The raw content of the file</param>
    /// <param name="contentType">The validated MIME type of the file</param>
    /// <param name="fileKey">The logical key (path) under which the file is stored</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<string> UploadAsync(byte[] content, string contentType, string fileKey, CancellationToken cancellationToken);

    /// <summary>
    ///     Builds a signed URL that gives temporary read access to a stored file.
    /// </summary>
    string GetTemporaryUrl(string storageReference, TimeSpan validFor);

    /// <summary>
    ///     Deletes a stored file.
    /// </summary>
    Task DeleteAsync(string storageReference, CancellationToken cancellationToken);
}