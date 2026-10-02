namespace SkillSwap.Platform.Reputation.Interfaces.Rest.Resources;

/// <summary>
///     Student employability resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the record</param>
/// <param name="StudentId">The student the score belongs to</param>
/// <param name="VerifiedSkillsCount">Skills the student certified, by automatic approval or by a verifier</param>
/// <param name="Score">Employability from 0 to 100: 10 points per certified skill</param>
/// <param name="UpdatedAt">When it was last recalculated (UTC)</param>
public record StudentEmployabilityResource(
    int Id,
    int StudentId,
    int VerifiedSkillsCount,
    int Score,
    DateTime UpdatedAt);