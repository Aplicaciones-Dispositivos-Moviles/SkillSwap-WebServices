namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.Queries;

/// <summary>
///     Get the movements of the wallet of a user, newest first
/// </summary>
/// <param name="OwnerId">The user who owns the wallet</param>
public record GetWalletTransactionsQuery(int OwnerId);