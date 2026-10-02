namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;

/// <summary>
///     Wallet resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the wallet</param>
/// <param name="WalletOwnerId">The user who owns the wallet</param>
/// <param name="Balance">The available SkillCredits</param>
public record WalletResource(int Id, int WalletOwnerId, int Balance);