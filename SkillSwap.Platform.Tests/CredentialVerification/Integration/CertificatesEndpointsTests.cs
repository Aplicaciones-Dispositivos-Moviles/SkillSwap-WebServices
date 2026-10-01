using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.CredentialVerification.Integration;

public class CertificatesEndpointsTests : ApiTestBase
{
    private const string Url = "/api/v1/certificates";

    private static (string Name, string Value)[] Ocr(string number = "cert-001", string code = "code-xyz")
    {
        return
        [
            ("holderName", "Ana Perez"), ("institutionName", "Coursera"), ("courseName", "Backend with ASP.NET"),
            ("issueDate", "2025-03-10"), ("durationHours", "40"), ("certificateNumber", number),
            ("verificationCode", code), ("verificationUrl", "https://example.com/verify/1")
        ];
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[]? file = null,
        string contentType = "image/jpeg", params (string Name, string Value)[] fields)
    {
        using var form = TestFiles.Form(file ?? TestFiles.Jpeg("default"), contentType, fields);
        return await client.PostAsync(Url, form);
    }

    private static async Task<CertificateResource> UploadOkAsync(HttpClient client, string seed,
        params (string Name, string Value)[] fields)
    {
        var response = await UploadAsync(client, TestFiles.Jpeg(seed), fields: fields);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CertificateResource>())!;
    }

    private static async Task<string?> ErrorTitle(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title;
    }

    // ---------- Upload ----------

    [Fact]
    public async Task Upload_WithValidFileAndOcrData_Returns201WithTheCertificate()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client, fields: Ocr());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var certificate = (await response.Content.ReadFromJsonAsync<CertificateResource>())!;
        Assert.Equal(ana.Id, certificate.OwnerId);
        Assert.Equal("Ana Perez", certificate.HolderName);
        Assert.Equal("Coursera", certificate.InstitutionName);
        Assert.Equal(new DateOnly(2025, 3, 10), certificate.IssueDate);
        Assert.Equal(40, certificate.DurationHours);
        Assert.Equal("CERT-001", certificate.CertificateNumber);
        Assert.Equal("CODE-XYZ", certificate.VerificationCode);
        Assert.Equal("Unverified", certificate.Status);
        Assert.Equal("OcrOnly", certificate.VerificationMethod);
        Assert.Equal("LowRisk", certificate.RiskLevel);
        Assert.Null(certificate.VerifiedAt);
        Assert.StartsWith($"https://files.test/stored/certificates/{ana.Id}/", certificate.FileUrl);
        Assert.Equal($"{Url}/{certificate.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Upload_WithoutAnyOcrData_Returns201AndCountsTheInconsistency()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var certificate = (await response.Content.ReadFromJsonAsync<CertificateResource>())!;
        Assert.Null(certificate.HolderName);
        Assert.Null(certificate.IssueDate);
        Assert.Equal("LowRisk", certificate.RiskLevel);
    }

    [Fact]
    public async Task Upload_IgnoresTheOwnerIdSentInTheForm()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client, fields: [("ownerId", "999")]);

        var certificate = (await response.Content.ReadFromJsonAsync<CertificateResource>())!;
        Assert.Equal(ana.Id, certificate.OwnerId);
    }

    [Fact]
    public async Task Upload_WithoutToken_Returns401()
    {
        var response = await UploadAsync(TestApi.CreateClient());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_AsCoordinator_Returns403()
    {
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");

        var response = await UploadAsync(coordinator.Client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutFile_Returns400FileRequired()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        using var form = TestFiles.Form(null, "image/jpeg", [("holderName", "Ana Perez")]);

        var response = await ana.Client.PostAsync(Url, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("FileRequired", await ErrorTitle(response));
    }

    [Fact]
    public async Task Upload_OfATextFile_Returns415()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client, "plain text"u8.ToArray(), "text/plain");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("InvalidFileType", await ErrorTitle(response));
    }

    [Fact]
    public async Task Upload_WhenTheDeclaredTypeDoesNotMatchTheContent_Returns415()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client, TestFiles.Jpeg("x"), "image/png");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OfAFileOverTenMegabytes_Returns413()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client, TestFiles.OverTenMegabytes());

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("FileTooLarge", await ErrorTitle(response));
    }

    [Fact]
    public async Task Upload_WithAFieldOverItsMaximumLength_Returns400()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client, fields: [("holderName", new string('a', 256))]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("FieldTooLong", await ErrorTitle(response));
    }

    [Fact]
    public async Task Upload_OfTheSameFileTwice_Returns409ReferencingTheExistingCertificate()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var first = await UploadOkAsync(ana.Client, "same-file");

        var again = await UploadAsync(ana.Client, TestFiles.Jpeg("same-file"));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        var problem = (await again.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal("DuplicateFile", problem.Title);
        Assert.Equal(first.Id, ((JsonElement)problem.Extensions["existingCertificateId"]!).GetInt32());
    }

    [Fact]
    public async Task Upload_OfAFileAnotherStudentAlreadyRegistered_IsSuspicious()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");
        await UploadOkAsync(ana.Client, "shared-file");

        var certificate = await UploadOkAsync(bob.Client, "shared-file");

        Assert.Equal("Suspicious", certificate.Status);
        Assert.Equal("HighRisk", certificate.RiskLevel);
    }

    [Fact]
    public async Task Upload_WithTheNumberAndCodeOfAnotherStudent_IsSuspicious()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");
        await UploadOkAsync(ana.Client, "file-a", Ocr());

        var certificate = await UploadOkAsync(bob.Client, "file-b", Ocr());

        Assert.Equal("Suspicious", certificate.Status);
        Assert.Equal("HighRisk", certificate.RiskLevel);
    }

    [Fact]
    public async Task Upload_WithOnlyTheNumberOfAnotherStudent_NeedsReviewButIsNotSuspicious()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");
        await UploadOkAsync(ana.Client, "file-a", Ocr(code: "code-a"));

        var certificate = await UploadOkAsync(bob.Client, "file-b", Ocr(code: "code-b"));

        Assert.Equal("Unverified", certificate.Status);
        Assert.Equal("Review", certificate.RiskLevel);
    }

    [Fact]
    public async Task Responses_NeverExposeInternalData()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await UploadAsync(ana.Client, fields: [.. Ocr(), ("ocrText", "secret text"), ("qrPayload", "qr")]);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var internalField in new[] { "fileHash", "storageReference", "ocrText", "qrPayload", "riskScore" })
            Assert.False(json.RootElement.TryGetProperty(internalField, out _), internalField);
    }

    [Fact]
    public async Task Upload_ErrorMessagesFollowTheAcceptLanguageHeader()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        using var form = TestFiles.Form("plain text"u8.ToArray(), "text/plain", []);
        using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = form };
        request.Headers.AcceptLanguage.ParseAdd("es-PE");

        var response = await ana.Client.SendAsync(request);

        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal("Solo se aceptan archivos JPG, PNG y PDF.", problem.Detail);
    }

    // ---------- Read ----------

    [Fact]
    public async Task GetById_AsOwner_Returns200()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var uploaded = await UploadOkAsync(ana.Client, "file-a", Ocr());

        var response = await ana.Client.GetAsync($"{Url}/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var certificate = (await response.Content.ReadFromJsonAsync<CertificateResource>())!;
        Assert.Equal(uploaded.Id, certificate.Id);
        Assert.Equal("Unverified", certificate.Status);
    }

    [Fact]
    public async Task GetById_AsAnotherStudent_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");
        var uploaded = await UploadOkAsync(ana.Client, "file-a");

        var response = await bob.Client.GetAsync($"{Url}/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotCertificateOwner", await ErrorTitle(response));
    }

    [Fact]
    public async Task GetById_AsCoordinator_Returns200()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");
        var uploaded = await UploadOkAsync(ana.Client, "file-a");

        var response = await coordinator.Client.GetAsync($"{Url}/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.GetAsync($"{Url}/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CertificateNotFound", await ErrorTitle(response));
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync($"{Url}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutOwnerId_ReturnsTheCallersCertificatesNewestFirst()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");
        var first = await UploadOkAsync(ana.Client, "file-1");
        await UploadOkAsync(bob.Client, "file-2");
        var third = await UploadOkAsync(ana.Client, "file-3");

        var certificates = (await ana.Client.GetFromJsonAsync<List<CertificateResource>>(Url))!;

        Assert.Equal([third.Id, first.Id], certificates.Select(c => c.Id));
    }

    [Fact]
    public async Task List_OfAnotherStudent_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await bob.Client.GetAsync($"{Url}?ownerId={ana.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_AsCoordinator_ReturnsTheRequestedStudentsCertificates()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");
        await UploadOkAsync(ana.Client, "file-1");

        var certificates =
            (await coordinator.Client.GetFromJsonAsync<List<CertificateResource>>($"{Url}?ownerId={ana.Id}"))!;

        Assert.Single(certificates);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync(Url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}