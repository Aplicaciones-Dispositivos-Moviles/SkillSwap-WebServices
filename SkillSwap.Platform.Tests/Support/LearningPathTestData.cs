using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;

namespace SkillSwap.Platform.Tests.Support;

public static class LearningPathTestData
{
    public static readonly FakeSkillTaxonomy Taxonomy = FakeSkillTaxonomy.Sample();

    public static CareerGoal Goal(params string[] tags)
    {
        return new CareerGoal("I want to build APIs", tags);
    }

    /// <summary>
    ///     Builds a path through the real analyzer and builder and assigns node ids 1..n in path order,
    ///     as the database would. For the goal "authentication-jwt" the nodes are:
    ///     1 networking-basics, 2 programming-fundamentals (both available), 3 http-basics,
    ///     4 rest-api-design, 5 authentication-jwt (locked).
    /// </summary>
    public static LearningPath NewPath(int studentId = 1, params string[] goalTags)
    {
        var goal = Goal(goalTags.Length == 0 ? ["authentication-jwt"] : goalTags);
        var gap = new SkillGapAnalyzer(Taxonomy).Analyze(goal, []);
        var path = new LearningPath(studentId, goal, new LearningPathBuilder(Taxonomy).BuildPath(gap));
        AssignNodeIds(path);
        return path;
    }
    
    /// <summary>
    ///     Same path as <see cref="NewPath" />, but the nodes keep no id: the database assigns them on save.
    /// </summary>
    public static LearningPath NewUnsavedPath(int studentId, params string[] goalTags)
    {
        var goal = Goal(goalTags.Length == 0 ? ["authentication-jwt"] : goalTags);
        var gap = new SkillGapAnalyzer(Taxonomy).Analyze(goal, []);
        return new LearningPath(studentId, goal, new LearningPathBuilder(Taxonomy).BuildPath(gap));
    }

    public static void AssignNodeIds(LearningPath path)
    {
        var id = 1;
        foreach (var node in path.Nodes) typeof(PathNode).GetProperty(nameof(PathNode.Id))!.SetValue(node, id++);
    }

    public static Question Question(int index)
    {
        return new Question($"Question {index}?", [$"a{index}", $"b{index}", $"c{index}", $"d{index}"], index % 4);
    }

    public static List<Question> Questions(int count)
    {
        return Enumerable.Range(1, count).Select(Question).ToList();
    }
}