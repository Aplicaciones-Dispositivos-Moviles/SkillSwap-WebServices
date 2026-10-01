using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkillSwap.Platform.CredentialVerification.Application.Internal.OutboundServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;

namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Hosts the real API in memory, wired to the test database and to fake file storage and fake AI.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestSecret = "integration-tests-secret-with-more-than-32-characters";

    public ApiFactory()
    {
        // Environment variables override appsettings and are read when the host is created,
        // so the API never touches the development database, the real Cloudinary or the real Gemini.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", TestDatabase.ConnectionString);
        Environment.SetEnvironmentVariable("TokenSettings__Secret", TestSecret);
        Environment.SetEnvironmentVariable("Cloudinary__CloudName", "test-cloud");
        Environment.SetEnvironmentVariable("Cloudinary__ApiKey", "test-key");
        Environment.SetEnvironmentVariable("Cloudinary__ApiSecret", "test-secret");
        Environment.SetEnvironmentVariable("Gemini__ApiKey", "test-gemini-key");
        Environment.SetEnvironmentVariable("Gemini__Model", "test-model");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService, FakeFileStorageService>();

            services.RemoveAll<IQuestionGenerationService>();
            services.AddSingleton<IQuestionGenerationService, FakeQuestionGenerationService>();
        });
    }
}