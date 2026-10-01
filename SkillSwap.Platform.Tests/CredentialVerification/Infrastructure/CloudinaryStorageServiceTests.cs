using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SkillSwap.Platform.CredentialVerification.Infrastructure.FileStorage.Configuration;
using SkillSwap.Platform.CredentialVerification.Infrastructure.FileStorage.Services;

namespace SkillSwap.Platform.Tests.CredentialVerification.Infrastructure;

/// <summary>
///     Only the parts that need no network. Uploading and deleting are checked manually against Cloudinary.
/// </summary>
public class CloudinaryStorageServiceTests
{
    private const string Secret = "secret456";

    private static CloudinaryStorageService Create()
    {
        return new CloudinaryStorageService(Options.Create(new CloudinarySettings
            { CloudName = "demo", ApiKey = "key123", ApiSecret = Secret }));
    }

    [Fact]
    public void GetTemporaryUrl_BuildsASignedPrivateDownloadUrl()
    {
        var url = Create().GetTemporaryUrl("certificates/7/abc.pdf", TimeSpan.FromMinutes(15));

        var uri = new Uri(url);
        Assert.Equal("api.cloudinary.com", uri.Host);
        Assert.Equal("/v1_1/demo/image/download", uri.AbsolutePath);

        var query = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal("certificates/7/abc", query["public_id"].ToString());
        Assert.Equal("pdf", query["format"].ToString());
        Assert.Equal("authenticated", query["type"].ToString());
        Assert.Equal("key123", query["api_key"].ToString());

        var expiresAt = long.Parse(query["expires_at"].ToString());
        var expected = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        Assert.InRange(expiresAt, expected - 5, expected + 5);
    }

    [Fact]
    public void GetTemporaryUrl_SignsTheParametersAndNeverExposesTheSecret()
    {
        var url = Create().GetTemporaryUrl("certificates/7/abc.pdf", TimeSpan.FromMinutes(15));
        var query = QueryHelpers.ParseQuery(new Uri(url).Query);

        var toSign = $"expires_at={query["expires_at"]}&format=pdf&public_id=certificates/7/abc" +
                     $"&timestamp={query["timestamp"]}&type=authenticated{Secret}";
        var expectedSignature = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(toSign))).ToLowerInvariant();

        Assert.Equal(expectedSignature, query["signature"].ToString());
        Assert.DoesNotContain(Secret, url);
    }

    [Theory]
    [InlineData("no-extension")]
    [InlineData(".hidden")]
    [InlineData("trailing.")]
    public void GetTemporaryUrl_WithAnInvalidReference_Throws(string reference)
    {
        Assert.Throws<ArgumentException>(() => Create().GetTemporaryUrl(reference, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task UploadAsync_WithAnUnsupportedContentType_ThrowsBeforeAnyNetworkCall()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Create().UploadAsync([1, 2, 3], "text/plain", "certificates/1/x", CancellationToken.None));
    }
}