using System.Net;
using System.Net.Http.Json;
using Reqnroll;
using SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Iam.Steps;
using SkillSwap.Platform.Tests.Support;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SkillSwap.Platform.Tests.CredentialVerification.Steps;

[Binding]
public class CertificateSteps(ApiScenarioContext context)
{
    private const string Url = "/api/v1/certificates";

    private async Task UploadAsync(string username, byte[]? file, string contentType,
        params (string Name, string Value)[] fields)
    {
        using var form = TestFiles.Form(file, contentType, fields);
        context.Response = await context.Users[username].Client.PostAsync(Url, form);

        if (context.Response.StatusCode == HttpStatusCode.Created)
            context.CertificateIds[username] = (await context.ReadBodyAsync<CertificateResource>()).Id;
    }

    private Task UploadNamedAsync(string username, string fileName, string? number = null, string? code = null)
    {
        var fields = new List<(string Name, string Value)>();
        if (number is not null) fields.Add(("certificateNumber", number));
        if (code is not null) fields.Add(("verificationCode", code));
        return UploadAsync(username, TestFiles.Jpeg(fileName), "image/jpeg", fields.ToArray());
    }

    // ---------- Given / When: uploads ----------

    [When("{string} uploads a {word} certificate file")]
    public async Task WhenUploadsACertificateOfFormat(string username, string format)
    {
        var (file, contentType) = format switch
        {
            "JPEG" => (TestFiles.Jpeg("format-jpeg"), "image/jpeg"),
            "PNG" => (TestFiles.Png("format-png"), "image/png"),
            "PDF" => (TestFiles.Pdf("format-pdf"), "application/pdf"),
            "text" => ("plain text"u8.ToArray(), "text/plain"),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown file format.")
        };
        await UploadAsync(username, file, contentType);
    }

    [When("{string} uploads a certificate file larger than 10 MB")]
    public async Task WhenUploadsALargeFile(string username)
    {
        await UploadAsync(username, TestFiles.OverTenMegabytes(), "image/jpeg");
    }

    [When("{string} uploads a request without a file")]
    public async Task WhenUploadsWithoutAFile(string username)
    {
        await UploadAsync(username, null, "image/jpeg", ("holderName", "Ana Perez"));
    }

    [When("{string} uploads the certificate file {string}")]
    public async Task WhenUploadsTheCertificateFile(string username, string fileName)
    {
        await UploadNamedAsync(username, fileName);
    }

    [Given("{string} has uploaded the certificate file {string}")]
    public async Task GivenHasUploadedTheCertificateFile(string username, string fileName)
    {
        await UploadNamedAsync(username, fileName);
        Assert.Equal(HttpStatusCode.Created, context.Response!.StatusCode);
    }

    [When("{string} uploads the certificate file {string} with the number {string} and the code {string}")]
    public async Task WhenUploadsTheCertificateFileWithNumberAndCode(string username, string fileName,
        string number, string code)
    {
        await UploadNamedAsync(username, fileName, number, code);
    }

    [Given("{string} has uploaded the certificate file {string} with the number {string} and the code {string}")]
    public async Task GivenHasUploadedTheCertificateFileWithNumberAndCode(string username, string fileName,
        string number, string code)
    {
        await UploadNamedAsync(username, fileName, number, code);
        Assert.Equal(HttpStatusCode.Created, context.Response!.StatusCode);
    }

    // ---------- When: reads ----------

    [When("{string} lists the certificates of {string}")]
    public async Task WhenListsTheCertificatesOf(string actor, string owner)
    {
        context.Response = await context.Users[actor].Client.GetAsync($"{Url}?ownerId={context.Users[owner].Id}");
    }

    [When("{string} opens the last certificate of {string}")]
    public async Task WhenOpensTheLastCertificateOf(string actor, string owner)
    {
        context.Response = await context.Users[actor].Client.GetAsync($"{Url}/{context.CertificateIds[owner]}");
    }

    // ---------- Then ----------

    [Then("the certificate status is {string}")]
    public async Task ThenTheCertificateStatusIs(string status)
    {
        Assert.Equal(status, (await context.ReadBodyAsync<CertificateResource>()).Status);
    }

    [Then("the risk level is {string}")]
    public async Task ThenTheRiskLevelIs(string riskLevel)
    {
        Assert.Equal(riskLevel, (await context.ReadBodyAsync<CertificateResource>()).RiskLevel);
    }

    [Then("the response contains {int} certificates")]
    public async Task ThenTheResponseContainsCertificates(int count)
    {
        Assert.Equal(count, (await context.ReadBodyAsync<List<CertificateResource>>()).Count);
    }
    [Then("the error references the existing certificate of {string}")]
    public async Task ThenTheErrorReferencesTheExistingCertificateOf(string owner)
    {
        var problem = await context.ReadBodyAsync<ProblemDetails>();
        var referenced = ((JsonElement)problem.Extensions["existingCertificateId"]!).GetInt32();
        Assert.Equal(context.CertificateIds[owner], referenced);
    }
}