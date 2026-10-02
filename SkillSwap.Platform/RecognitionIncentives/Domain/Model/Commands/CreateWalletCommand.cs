namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;

/// <summary>
///     Create wallet command
/// </summary>
/// <param name="OwnerId">The user who owns the wallet</param>
public record CreateWalletCommand(int OwnerId);