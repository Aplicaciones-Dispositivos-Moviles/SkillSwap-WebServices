using SkillSwap.Platform.CredentialVerification.Application.Internal.OutboundServices;

namespace SkillSwap.Platform.Tests.Support;

public class FakeFileStorageService : IFileStorageService
{
    public List<(string Key, string ContentType, int Length)> Uploads { get; } = [];
    public List<string> Deleted { get; } = [];

    /// <summary>
    ///     When set, <see cref="UploadAsync" /> throws it, simulating a storage outage.
    /// </summary>
    public Exception? UploadException { get; set; }

    public Task<string> UploadAsync(byte[] content, string contentType, string fileKey,
        CancellationToken cancellationToken)
    {
        if (UploadException is not null) throw UploadException;

        Uploads.Add((fileKey, contentType, content.Length));
        return Task.FromResult($"stored/{fileKey}");
    }

    public string GetTemporaryUrl(string storageReference, TimeSpan validFor)
    {
        return $"https://files.test/{storageReference}?minutes={(int)validFor.TotalMinutes}";
    }

    public Task DeleteAsync(string storageReference, CancellationToken cancellationToken)
    {
        Deleted.Add(storageReference);
        return Task.CompletedTask;
    }
}