using Microsoft.AspNetCore.Mvc.Testing;

namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Hosts the real API in memory, wired to the test database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestSecret = "integration-tests-secret-with-more-than-32-characters";

    public ApiFactory()
    {
        // Environment variables override appsettings and are read when the host is created,
        // so the API never touches the development database during tests.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", TestDatabase.ConnectionString);
        Environment.SetEnvironmentVariable("TokenSettings__Secret", TestSecret);
    }
}