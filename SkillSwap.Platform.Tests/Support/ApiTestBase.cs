namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Starts every test with empty tables.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await TestApi.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}