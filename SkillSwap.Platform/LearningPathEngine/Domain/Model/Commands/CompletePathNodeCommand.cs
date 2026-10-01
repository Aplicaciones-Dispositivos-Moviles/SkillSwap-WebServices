namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;

/// <summary>
///     Complete path node command. Issued by Assessment &amp; Peer Review (through the facade) once the
///     student approved the node's assessment; it is not exposed through REST.
/// </summary>
/// <param name="PathNodeId">The node that was demonstrated</param>
public record CompletePathNodeCommand(int PathNodeId);