using SkillSwap.Platform.LearningPathEngine.Application.ACL;

namespace SkillSwap.Platform.Tests.Support;

public class FakeLearningPathContextFacade : ILearningPathContextFacade
{
    /// <summary>
    ///     The correct answers of the blueprints built by <see cref="AddBlueprint" />.
    /// </summary>
    public static readonly int[] Correct = [1, 2, 3, 0, 1];

    public Dictionary<int, BlueprintView> Blueprints { get; } = [];

    public HashSet<(int StudentId, string SkillTag)> CompletedSkills { get; } = [];

    public List<int> CompletedNodes { get; } = [];

    /// <summary>
    ///     What <see cref="CompleteNodeAsync" /> reports; the node is recorded only when it is Completed.
    /// </summary>
    public NodeCompletionOutcome NextOutcome { get; set; } = NodeCompletionOutcome.Completed;

    public BlueprintView AddBlueprint(int blueprintId = 1, int pathNodeId = 10, int studentId = 1,
        string skillTag = "http-basics", bool isLatest = true, bool nodeIsAvailable = true)
    {
        var questions = Correct
            .Select((correct, index) => new BlueprintQuestionView($"Question {index + 1}?",
                [$"a{index + 1}", $"b{index + 1}", $"c{index + 1}", $"d{index + 1}"], correct))
            .ToList();
        var view = new BlueprintView(blueprintId, pathNodeId, studentId, skillTag, isLatest, nodeIsAvailable,
            questions);
        Blueprints[blueprintId] = view;
        return view;
    }

    public Task<BlueprintView?> GetBlueprintAsync(int blueprintId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Blueprints.GetValueOrDefault(blueprintId));
    }

    public Task<NodeCompletionOutcome> CompleteNodeAsync(int pathNodeId, CancellationToken cancellationToken)
    {
        if (NextOutcome == NodeCompletionOutcome.Completed) CompletedNodes.Add(pathNodeId);
        return Task.FromResult(NextOutcome);
    }

    public Task<bool> HasCompletedSkillAsync(int studentId, string skillTag, CancellationToken cancellationToken)
    {
        return Task.FromResult(CompletedSkills.Contains((studentId, skillTag)));
    }
}