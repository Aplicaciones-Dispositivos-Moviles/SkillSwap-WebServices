using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Services;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace SkillSwap.Platform.Tests.Support;

public sealed record SignedInUser(int Id, string Username, HttpClient Client);

/// <summary>
///     Single shared API instance and helpers for integration and BDD tests.
///     Test parallelization is disabled, so one database is safe to share.
/// </summary>
public static class TestApi
{
    public const string DefaultPassword = "password123";

    private const string TruncateAllTables = """
                                             DO $$
                                             DECLARE r RECORD;
                                             BEGIN
                                                 FOR r IN (SELECT tablename FROM pg_tables
                                                           WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory')
                                                 LOOP
                                                     EXECUTE 'TRUNCATE TABLE ' || quote_ident(r.tablename) || ' RESTART IDENTITY CASCADE';
                                                 END LOOP;
                                             END $$;
                                             """;

    private static readonly Lazy<ApiFactory> LazyFactory = new(CreateFactory);

    public static ApiFactory Factory => LazyFactory.Value;

    private static ApiFactory CreateFactory()
    {
        var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        // Applies the real migrations (and creates the test database if it does not exist).
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        return factory;
    }

    public static HttpClient CreateClient()
    {
        return Factory.CreateClient();
    }

    public static IServiceScope CreateScope()
    {
        return Factory.Services.CreateScope();
    }

    public static async Task ResetDatabaseAsync()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.ExecuteSqlRawAsync(TruncateAllTables);
    }

    public static string EmailFor(string username)
    {
        return $"{username.ToLowerInvariant()}@upc.edu.pe";
    }

    public static async Task<SignedInUser> RegisterStudentAsync(string username)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/v1/authentication/sign-up",
            new SignUpResource(username, EmailFor(username), DefaultPassword));
        response.EnsureSuccessStatusCode();
        return await SignInAsync(username);
    }

    /// <summary>
    ///     Coordinators cannot self-register, so the account is inserted directly in the database.
    /// </summary>
    public static async Task<SignedInUser> RegisterCoordinatorAsync(string username)
    {
        using (var scope = CreateScope())
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Set<User>().Add(new User(new Username(username), new Email(EmailFor(username)),
                hasher.HashPassword(DefaultPassword), UserRole.Coordinator));
            await context.SaveChangesAsync();
        }

        return await SignInAsync(username);
    }

    public static async Task<SignedInUser> SignInAsync(string username, string password = DefaultPassword)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new SignInResource(username, password));
        response.EnsureSuccessStatusCode();

        var authenticated = await response.Content.ReadFromJsonAsync<AuthenticatedUserResource>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticated!.Token);
        return new SignedInUser(authenticated.Id, authenticated.Username, client);
    }
}