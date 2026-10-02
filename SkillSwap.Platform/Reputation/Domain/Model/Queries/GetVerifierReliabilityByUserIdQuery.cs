namespace SkillSwap.Platform.Reputation.Domain.Model.Queries;

/// <summary>
///     Get the reliability of a verifier
/// </summary>
/// <param name="VerifierUserId">The verifier's user id</param>
public record GetVerifierReliabilityByUserIdQuery(int VerifierUserId);