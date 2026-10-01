// UpdateVerifierAvailabilityCommand.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;

/// <summary>
///     Update verifier availability command
/// </summary>
/// <param name="UserId">The authenticated verifier</param>
/// <param name="Available">Whether the verifier accepts new cases</param>
public record UpdateVerifierAvailabilityCommand(int UserId, bool Available);