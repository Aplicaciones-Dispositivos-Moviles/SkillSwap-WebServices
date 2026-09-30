namespace SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>
///     Resource for updating the profile description
/// </summary>
/// <param name="Bio">The new bio text (up to 1000 characters)</param>
public record UpdateUserBioResource(string Bio);