using System.Security.Cryptography;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SkillSwap.Platform.CredentialVerification.Application.Internal.OutboundServices;
using SkillSwap.Platform.CredentialVerification.Infrastructure.FileStorage.Configuration;

namespace SkillSwap.Platform.CredentialVerification.Infrastructure.FileStorage.Services;

/// <summary>
///     Cloudinary implementation of <see cref="IFileStorageService" />. Files are uploaded with the
///     "authenticated" delivery type, so they have no public URL: the only way to read one is a
///     signed private-download URL that expires.
/// </summary>
/// <remarks>
///     The storage reference is "{publicId}.{extension}", e.g. "certificates/7/3fa1....pdf".
///     PDFs are stored as Cloudinary "image" resources, like JPG and PNG.
/// </remarks>
public class CloudinaryStorageService : IFileStorageService
{
    private const string DeliveryType = "authenticated";

    private static readonly Dictionary<string, string> Extensions = new()
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["application/pdf"] = "pdf"
    };

    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;

    public CloudinaryStorageService(IOptions<CloudinarySettings> options)
    {
        _settings = options.Value;
        _cloudinary = new Cloudinary(new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret));
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(byte[] content, string contentType, string fileKey,
        CancellationToken cancellationToken)
    {
        if (!Extensions.TryGetValue(contentType, out var extension))
            throw new ArgumentException($"Unsupported content type '{contentType}'.", nameof(contentType));

        await using var stream = new MemoryStream(content);
        var parameters = new ImageUploadParams
        {
            File = new FileDescription(fileKey, stream),
            PublicId = fileKey,
            Type = DeliveryType,
            Overwrite = false,
            UseFilename = false,
            UniqueFilename = false
        };

        var result = await _cloudinary.UploadAsync(parameters, cancellationToken);
        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return $"{result.PublicId}.{extension}";
    }

    /// <inheritdoc />
    public string GetTemporaryUrl(string storageReference, TimeSpan validFor)
    {
        var (publicId, format) = Parse(storageReference);
        var now = DateTimeOffset.UtcNow;

        // Cloudinary "private download" URL: the signature covers the parameters sorted by name
        // (raw, unencoded values) followed by the API secret, hashed with SHA-1.
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["expires_at"] = now.Add(validFor).ToUnixTimeSeconds().ToString(),
            ["format"] = format,
            ["public_id"] = publicId,
            ["timestamp"] = now.ToUnixTimeSeconds().ToString(),
            ["type"] = DeliveryType
        };

        var toSign = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}")) + _settings.ApiSecret;
        var signature = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(toSign))).ToLowerInvariant();

        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))
                    + $"&api_key={Uri.EscapeDataString(_settings.ApiKey)}&signature={signature}";
        return $"https://api.cloudinary.com/v1_1/{_settings.CloudName}/image/download?{query}";
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string storageReference, CancellationToken cancellationToken)
    {
        var (publicId, _) = Parse(storageReference);
        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image,
            Type = DeliveryType,
            Invalidate = true
        });

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary delete failed: {result.Error.Message}");
    }

    private static (string PublicId, string Format) Parse(string storageReference)
    {
        var dot = storageReference.LastIndexOf('.');
        if (dot <= 0 || dot == storageReference.Length - 1)
            throw new ArgumentException($"Invalid storage reference '{storageReference}'.", nameof(storageReference));

        return (storageReference[..dot], storageReference[(dot + 1)..]);
    }
}