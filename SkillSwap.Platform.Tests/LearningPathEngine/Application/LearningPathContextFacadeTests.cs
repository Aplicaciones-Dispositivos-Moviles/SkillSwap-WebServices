using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.LearningPathEngine.Application.ACL;
using SkillSwap.Platform.LearningPathEngine.Application.Internal.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Application;

public class LearningPathContextFacadeTests
{
    private readonly LearningPathContextFacade _facade;
    private readonly FakeAssessmentBlueprintRepository _blueprints = new();
    private readonly FakeLearningPathRepository _paths = new();

    public LearningPathContextFacadeTests()
    {
        var commandService = new LearningPathCommandService(
            _paths,
            FakeSkillTaxonomyMatcher.Sample(),
            new SkillGapAnalyzer(LearningPathTestData.Taxonomy),
            new LearningPathBuilder(LearningPathTestData.Taxonomy),
            new FakeCredentialContextFacade(),
            new FakeUnitOfWork(),
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<LearningPathCommandService>.Instance);
        _facade = new LearningPathContextFacade(_paths, _blueprints, commandService);
    }

    private async Task<LearningPath> AddPathAsync(int studentId = 1)
    {
        var path = LearningPathTestData.NewPath(studentId);
        await _paths.AddAsync(path);
        return path;
    }

    private static PathNode AvailableNode(LearningPath path)
    {
        return path.Nodes.First(n => n.Status == NodeStatus.Available);
    }

    private async Task<AssessmentBlueprint> AddBlueprintAsync(LearningPath path, PathNode node)
    {
        var blueprint = new AssessmentBlueprint(node.Id, node.SkillTag, LearningPathTestData.Questions(5));
        await _blueprints.AddAsync(blueprint);
        path.AttachBlueprint(node.Id, blueprint.Id);
        return blueprint;
    }

    // ---------- GetBlueprint ----------

    [Fact]
    public async Task GetBlueprint_ReturnsTheDataNeededToGradeAnAttempt()
    {
        var path = await AddPathAsync(studentId: 7);
        var node = AvailableNode(path);
        var blueprint = await AddBlueprintAsync(path, node);

        var view = await _facade.GetBlueprintAsync(blueprint.Id, CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal(blueprint.Id, view.BlueprintId);
        Assert.Equal(node.Id, view.PathNodeId);
        Assert.Equal(7, view.StudentId);
        Assert.Equal(node.SkillTag, view.SkillTag);
        Assert.True(view.IsLatest);
        Assert.True(view.NodeIsAvailable);
        Assert.Equal(blueprint.Questions.Select(q => q.CorrectAnswer), view.Questions.Select(q => q.CorrectAnswer));
        Assert.Equal(blueprint.Questions.Select(q => q.QuestionString), view.Questions.Select(q => q.Text));
    }

    [Fact]
    public async Task GetBlueprint_ForAnUnknownBlueprint_ReturnsNull()
    {
        Assert.Null(await _facade.GetBlueprintAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task GetBlueprint_OfAnOlderBlueprint_IsNotTheLatest()
    {
        var path = await AddPathAsync();
        var node = AvailableNode(path);
        var older = await AddBlueprintAsync(path, node);
        var latest = await AddBlueprintAsync(path, node);

        var olderView = await _facade.GetBlueprintAsync(older.Id, CancellationToken.None);
        var latestView = await _facade.GetBlueprintAsync(latest.Id, CancellationToken.None);

        Assert.False(olderView!.IsLatest);
        Assert.True(latestView!.IsLatest);
    }

    [Fact]
    public async Task GetBlueprint_OfACompletedNode_IsNoLongerAvailable()
    {
        var path = await AddPathAsync();
        var node = AvailableNode(path);
        var blueprint = await AddBlueprintAsync(path, node);
        await _facade.CompleteNodeAsync(node.Id, CancellationToken.None);

        var view = await _facade.GetBlueprintAsync(blueprint.Id, CancellationToken.None);

        Assert.False(view!.NodeIsAvailable);
    }

    // ---------- CompleteNode ----------

    [Fact]
    public async Task CompleteNode_OfAnAvailableNode_CompletesIt()
    {
        var path = await AddPathAsync();
        var node = AvailableNode(path);

        var outcome = await _facade.CompleteNodeAsync(node.Id, CancellationToken.None);

        Assert.Equal(NodeCompletionOutcome.Completed, outcome);
        Assert.Equal(NodeStatus.Completed, node.Status);
    }

    [Fact]
    public async Task CompleteNode_OfAnUnknownNode_ReportsNodeNotFound()
    {
        Assert.Equal(NodeCompletionOutcome.NodeNotFound, await _facade.CompleteNodeAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteNode_OfALockedNode_ReportsNodeLocked()
    {
        var path = await AddPathAsync();
        var locked = path.Nodes.First(n => n.Status == NodeStatus.Locked);

        var outcome = await _facade.CompleteNodeAsync(locked.Id, CancellationToken.None);

        Assert.Equal(NodeCompletionOutcome.NodeLocked, outcome);
        Assert.Equal(NodeStatus.Locked, locked.Status);
    }

    [Fact]
    public async Task CompleteNode_OfACompletedNode_ReportsAlreadyCompleted()
    {
        var path = await AddPathAsync();
        var node = AvailableNode(path);
        await _facade.CompleteNodeAsync(node.Id, CancellationToken.None);

        var outcome = await _facade.CompleteNodeAsync(node.Id, CancellationToken.None);

        Assert.Equal(NodeCompletionOutcome.AlreadyCompleted, outcome);
    }

    // ---------- HasCompletedSkill ----------

    [Fact]
    public async Task HasCompletedSkill_AfterCompletingTheNode_IsTrueOnlyForThatStudent()
    {
        var path = await AddPathAsync(studentId: 1);
        var node = AvailableNode(path);
        await _facade.CompleteNodeAsync(node.Id, CancellationToken.None);

        Assert.True(await _facade.HasCompletedSkillAsync(1, node.SkillTag, CancellationToken.None));
        Assert.False(await _facade.HasCompletedSkillAsync(2, node.SkillTag, CancellationToken.None));
    }

    [Fact]
    public async Task HasCompletedSkill_ForANodeStillPending_IsFalse()
    {
        var path = await AddPathAsync();
        var node = AvailableNode(path);

        Assert.False(await _facade.HasCompletedSkillAsync(1, node.SkillTag, CancellationToken.None));
    }
}