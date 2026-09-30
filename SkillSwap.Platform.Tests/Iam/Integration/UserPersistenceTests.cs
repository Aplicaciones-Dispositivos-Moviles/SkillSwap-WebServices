using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Repositories;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Integration;

public class UserPersistenceTests : ApiTestBase
{
    private static async Task Save(User user)
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IUserRepository>().AddAsync(user);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    [Fact]
    public async Task Repository_FindsAndChecksUsersThroughTheValueObjects()
    {
        await Save(TestData.NewUser(username: "ana", email: "ana@upc.edu.pe"));

        using var scope = TestApi.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        Assert.NotNull(await repository.FindByUsernameAsync(new Username("ANA"), default));
        Assert.NotNull(await repository.FindByEmailAsync(new Email("Ana@UPC.edu.pe"), default));
        Assert.True(await repository.ExistsByUsernameAsync(new Username("ana"), default));
        Assert.True(await repository.ExistsByEmailAsync(new Email("ana@upc.edu.pe"), default));
        Assert.False(await repository.ExistsByUsernameAsync(new Username("nobody"), default));
        Assert.False(await repository.ExistsByEmailAsync(new Email("nobody@upc.edu.pe"), default));
    }

    [Fact]
    public async Task Database_RejectsTwoUsersWithTheSameUsername()
    {
        await Save(TestData.NewUser(username: "ana", email: "ana@upc.edu.pe"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Save(TestData.NewUser(username: "ana", email: "other@upc.edu.pe")));
    }

    [Fact]
    public async Task Database_RejectsTwoUsersWithTheSameEmail()
    {
        await Save(TestData.NewUser(username: "ana", email: "ana@upc.edu.pe"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Save(TestData.NewUser(username: "other", email: "ana@upc.edu.pe")));
    }

    [Fact]
    public async Task DeviceToken_IsPersistedWhenPresentAndNullWhenAbsent()
    {
        var withToken = TestData.NewUser(username: "ana", email: "ana@upc.edu.pe");
        withToken.RegisterDeviceToken("device-abc");
        await Save(withToken);
        await Save(TestData.NewUser(username: "bob", email: "bob@upc.edu.pe"));

        using var scope = TestApi.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var ana = await repository.FindByUsernameAsync(new Username("ana"), default);
        var bob = await repository.FindByUsernameAsync(new Username("bob"), default);
        Assert.Equal(new DeviceToken("device-abc"), ana!.DeviceToken);
        Assert.Null(bob!.DeviceToken);
    }

    [Fact]
    public async Task Role_IsStoredAsText()
    {
        await Save(TestData.NewUser(username: "coord", email: "coord@upc.edu.pe", role: UserRole.Coordinator));

        using var scope = TestApi.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .FindByUsernameAsync(new Username("coord"), default);

        Assert.Equal(UserRole.Coordinator, user!.Role);
    }
}