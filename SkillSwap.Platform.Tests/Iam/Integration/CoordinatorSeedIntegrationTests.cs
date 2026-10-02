using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Repositories;
using SkillSwap.Platform.Iam.Domain.Services;
using SkillSwap.Platform.Iam.Infrastructure.Seeding;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Integration;

public class CoordinatorSeedIntegrationTests : ApiTestBase
{
    private static async Task<CoordinatorSeedOutcome> SeedAsync(string username, string email, string password)
    {
        using var scope = TestApi.CreateScope();
        var services = scope.ServiceProvider;
        var settings = new CoordinatorSeedSettings { Username = username, Email = email, Password = password };
        var seeder = new CoordinatorSeeder(
            services.GetRequiredService<IUserRepository>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<IUnitOfWork>(),
            Options.Create(settings),
            NullLogger<CoordinatorSeeder>.Instance);
        return await seeder.SeedAsync();
    }

    [Fact]
    public async Task Seed_CreatesACoordinatorWhoCanSignIn()
    {
        var outcome = await SeedAsync("root", "root@upc.edu.pe", "password123");

        Assert.Equal(CoordinatorSeedOutcome.Created, outcome);
        var coordinator = await TestApi.SignInAsync("root", "password123");
        Assert.True(coordinator.Id > 0);

        using var scope = TestApi.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .FindByUsernameAsync(new Username("root"), default);
        Assert.Equal(UserRole.Coordinator, stored!.Role);
    }

    [Fact]
    public async Task Seed_RunTwice_KeepsASingleAccount()
    {
        await SeedAsync("root", "root@upc.edu.pe", "password123");

        var second = await SeedAsync("root", "root@upc.edu.pe", "password123");

        Assert.Equal(CoordinatorSeedOutcome.AlreadyExists, second);
        using var scope = TestApi.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<IUserRepository>().ListAsync());
    }
}