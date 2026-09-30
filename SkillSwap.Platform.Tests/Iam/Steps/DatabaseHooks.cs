using Reqnroll;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Steps;

[Binding]
public static class DatabaseHooks
{
    [BeforeScenario]
    public static async Task ResetDatabase()
    {
        await TestApi.ResetDatabaseAsync();
    }
}