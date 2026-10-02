namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.Queries;

/// <summary>
///     Get the wallet of a user
/// </summary>
/// <param name="OwnerId">The user who owns the wallet</param>
public record GetWalletByOwnerIdQuery(int OwnerId);