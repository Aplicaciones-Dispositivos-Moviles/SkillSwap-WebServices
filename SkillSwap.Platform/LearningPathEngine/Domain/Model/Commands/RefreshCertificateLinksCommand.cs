namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;

/// <summary>
///     Refresh certificate links command: links to the nodes of the student's active path the certificates
///     the student uploaded after the path was created.
/// </summary>
/// <param name="StudentId">The student who owns the path</param>
public record RefreshCertificateLinksCommand(int StudentId);