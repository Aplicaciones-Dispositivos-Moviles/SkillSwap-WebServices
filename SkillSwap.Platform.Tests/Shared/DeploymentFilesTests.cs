namespace SkillSwap.Platform.Tests.Shared;

public class DeploymentFilesTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SkillSwap.Platform.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root (SkillSwap.Platform.sln).");
    }

    private static string ReadFile(string name)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot(), name));
    }

    [Fact]
    public void Dockerfile_PublishesTheApiAndListensOnThePortRenderProvides()
    {
        var dockerfile = ReadFile("Dockerfile");

        Assert.Contains("dotnet publish SkillSwap.Platform/SkillSwap.Platform.csproj", dockerfile);
        Assert.Contains("${PORT", dockerfile);
        Assert.Contains("ASPNETCORE_ENVIRONMENT=Production", dockerfile);
    }

    [Fact]
    public void Dockerfile_DoesNotBakeInSecretsOrTheDevelopmentSettings()
    {
        var dockerfile = ReadFile("Dockerfile");

        Assert.DoesNotContain("appsettings.Development", dockerfile);
        Assert.DoesNotContain("TokenSettings", dockerfile);
        Assert.DoesNotContain("ApiKey", dockerfile);
    }

    [Fact]
    public void DockerIgnore_KeepsTheDevelopmentSettingsAndTheTestsOutOfTheImage()
    {
        var lines = ReadFile(".dockerignore").Split('\n', StringSplitOptions.TrimEntries);

        Assert.Contains("**/appsettings.Development.json", lines);
        Assert.Contains("SkillSwap.Platform.Tests/", lines);
        Assert.Contains("**/bin/", lines);
        Assert.Contains("**/obj/", lines);
    }
}