// CreateVerifierProfileCommand.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;

/// <summary>
///     Create verifier profile command. Creates the profile or adds the skill to the existing one.
/// </summary>
/// <param name="UserId">The authenticated student</param>
/// <param name="SkillTag">The skill to be enabled for, which the student must have completed</param>
public record CreateVerifierProfileCommand(int UserId, string SkillTag);