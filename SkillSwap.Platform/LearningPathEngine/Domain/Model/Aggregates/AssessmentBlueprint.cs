using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;

/// <summary>
///     AssessmentBlueprint aggregate root
/// </summary>
/// <remarks>
///     The specification of the quiz generated to demonstrate the skill of a path node. Assessment &amp;
///     Peer Review consumes it to run and grade the attempt, without knowing how it was generated.
/// </remarks>
public class AssessmentBlueprint
{
    public const int QuestionCount = 5;

    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected AssessmentBlueprint()
    {
        SkillTag = null!;
        Questions = [];
    }

    public AssessmentBlueprint(int pathNodeId, string skillTag, IEnumerable<Question> questions)
    {
        if (pathNodeId <= 0)
            throw new DomainException("The blueprint must belong to a valid path node.");
        if (string.IsNullOrWhiteSpace(skillTag))
            throw new DomainException("The skill tag cannot be empty.");

        var list = questions.ToList();
        if (list.Count != QuestionCount)
            throw new DomainException($"An assessment needs exactly {QuestionCount} questions.");
        if (list.Select(q => q.QuestionString).Distinct(StringComparer.OrdinalIgnoreCase).Count() != list.Count)
            throw new DomainException("The questions of an assessment must be different from each other.");

        PathNodeId = pathNodeId;
        SkillTag = skillTag.Trim();
        Questions = list;
        GeneratedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int PathNodeId { get; private set; }
    public string SkillTag { get; private set; }
    public IReadOnlyList<Question> Questions { get; private set; }
    public DateTime GeneratedAt { get; private set; }
}