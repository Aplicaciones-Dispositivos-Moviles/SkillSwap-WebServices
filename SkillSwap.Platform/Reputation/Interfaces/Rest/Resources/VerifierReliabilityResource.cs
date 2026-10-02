namespace SkillSwap.Platform.Reputation.Interfaces.Rest.Resources;

/// <summary>
///     Verifier reliability resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the record</param>
/// <param name="VerifierUserId">The verifier the reliability belongs to</param>
/// <param name="ResolvedCasesCount">Cases the verifier resolved</param>
/// <param name="OverturnedDecisionsCount">Decisions of the verifier that were overturned after a dispute</param>
/// <param name="SanctionsCount">Sanctions applied to the verifier's account</param>
/// <param name="Score">Reliability from 0 to 100: starts at 100 and drops 15 per overturned decision and 25 per sanction</param>
/// <param name="UpdatedAt">When it was last recalculated (UTC)</param>
public record VerifierReliabilityResource(
    int Id,
    int VerifierUserId,
    int ResolvedCasesCount,
    int OverturnedDecisionsCount,
    int SanctionsCount,
    int Score,
    DateTime UpdatedAt);