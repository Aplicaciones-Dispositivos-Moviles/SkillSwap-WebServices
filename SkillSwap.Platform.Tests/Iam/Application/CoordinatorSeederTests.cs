using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Seeding;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Application;

public class CoordinatorSeederTests
{
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeUserRepository _users = new();

    private CoordinatorSeeder Seeder(string? username, string? email, string? password)
    {
        var settings = new CoordinatorSeedSettings { Username = username, Email = email, Password = password };
        return new CoordinatorSeeder(_users, _hasher, _unitOfWork, Options.Create(settings),
            NullLogger<CoordinatorSeeder>.Instance);
    }

    private CoordinatorSeeder ValidSeeder()
    {
        return Seeder("Root", "root@upc.edu.pe", "password123");
    }

    [Fact]
    public async Task Seed_WithAValidConfiguration_CreatesTheCoordinator()
    {
        var outcome = await ValidSeeder().SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.Created, outcome);
        var coordinator = Assert.Single(_users.Users);
        Assert.Equal(UserRole.Coordinator, coordinator.Role);
        Assert.Equal("root", coordinator.Username.Value);
        Assert.Equal("root@upc.edu.pe", coordinator.Email.Value);
        Assert.True(_hasher.VerifyPassword("password123", coordinator.PasswordHash));
        Assert.Equal(1, _unitOfWork.CompleteCalls);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", " ", "")]
    public async Task Seed_WithoutAnyValue_DoesNothing(string? username, string? email, string? password)
    {
        var outcome = await Seeder(username, email, password).SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.NotConfigured, outcome);
        Assert.Empty(_users.Users);
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    [Theory]
    [InlineData("root", null, "password123")]
    [InlineData(null, "root@upc.edu.pe", "password123")]
    [InlineData("root", "root@upc.edu.pe", null)]
    public async Task Seed_WithAPartialConfiguration_IsInvalid(string? username, string? email, string? password)
    {
        var outcome = await Seeder(username, email, password).SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.Invalid, outcome);
        Assert.Empty(_users.Users);
    }

    [Theory]
    [InlineData("ro", "root@upc.edu.pe", "password123")]
    [InlineData("ro ot", "root@upc.edu.pe", "password123")]
    [InlineData("root", "root@gmail.com", "password123")]
    [InlineData("root", "root@upc.edu.pe", "short12")]
    public async Task Seed_WithInvalidValues_IsInvalid(string username, string email, string password)
    {
        var outcome = await Seeder(username, email, password).SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.Invalid, outcome);
        Assert.Empty(_users.Users);
    }

    [Fact]
    public async Task Seed_WithAPasswordOver72Bytes_IsInvalid()
    {
        var outcome = await Seeder("root", "root@upc.edu.pe", new string('a', 73)).SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.Invalid, outcome);
        Assert.Empty(_users.Users);
    }

    [Fact]
    public async Task Seed_WhenTheUsernameExists_LeavesTheAccountUntouched()
    {
        await _users.AddAsync(new User(new Username("root"), new Email("other@upc.edu.pe"),
            _hasher.HashPassword("whatever123"), UserRole.Student));

        var outcome = await ValidSeeder().SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.AlreadyExists, outcome);
        var existing = Assert.Single(_users.Users);
        Assert.Equal(UserRole.Student, existing.Role);
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Seed_WhenTheEmailExists_LeavesTheAccountUntouched()
    {
        await _users.AddAsync(new User(new Username("someone"), new Email("root@upc.edu.pe"),
            _hasher.HashPassword("whatever123"), UserRole.Student));

        var outcome = await ValidSeeder().SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.AlreadyExists, outcome);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Seed_RunTwice_CreatesTheCoordinatorOnlyOnce()
    {
        await ValidSeeder().SeedAsync();

        var second = await ValidSeeder().SeedAsync();

        Assert.Equal(CoordinatorSeedOutcome.AlreadyExists, second);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Seed_WhenPersistenceFails_StopsTheStartup()
    {
        _unitOfWork.ExceptionToThrow = new InvalidOperationException("database down");

        await Assert.ThrowsAsync<InvalidOperationException>(() => ValidSeeder().SeedAsync());
    }
}