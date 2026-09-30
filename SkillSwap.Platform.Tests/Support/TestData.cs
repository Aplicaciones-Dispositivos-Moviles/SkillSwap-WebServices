using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Tests.Support;

public static class TestData
{
    /// <summary>
    ///     Builds a user and, optionally, forces its database-generated id.
    /// </summary>
    public static User NewUser(int? id = null, string username = "ana", string email = "ana@upc.edu.pe",
        UserRole role = UserRole.Student)
    {
        var user = new User(new Username(username), new Email(email), new PasswordHash("hash"), role);
        if (id is not null) SetId(user, id.Value);
        return user;
    }

    public static void SetId(User user, int id)
    {
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
    }
}