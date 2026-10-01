using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Domain;

public class LearningPathBuilderTests
{
    private readonly LearningPathBuilder _builder = new(LearningPathTestData.Taxonomy);

    private static SkillGap FullGap()
    {
        return new SkillGapAnalyzer(LearningPathTestData.Taxonomy)
            .Analyze(LearningPathTestData.Goal("authentication-jwt"), []);
    }

    [Fact]
    public void BuildPath_OrdersTheSkillsByPrerequisites()
    {
        var nodes = _builder.BuildPath(FullGap());

        Assert.Equal(
            ["networking-basics", "programming-fundamentals", "http-basics", "rest-api-design", "authentication-jwt"],
            nodes.Select(n => n.SkillTag));
        Assert.Equal([1, 2, 3, 4, 5], nodes.Select(n => n.Order));
    }

    [Fact]
    public void BuildPath_StartsAvailableOnlyTheNodesWithoutPendingPrerequisites()
    {
        var nodes = _builder.BuildPath(FullGap());

        Assert.Equal(
            [NodeStatus.Available, NodeStatus.Available, NodeStatus.Locked, NodeStatus.Locked, NodeStatus.Locked],
            nodes.Select(n => n.Status));
    }

    [Fact]
    public void BuildPath_KeepsOnlyThePrerequisitesThatAreInThePath()
    {
        var nodes = _builder.BuildPath(FullGap());

        var rest = nodes.Single(n => n.SkillTag == "rest-api-design");
        Assert.Equal(["http-basics", "programming-fundamentals"], rest.PrerequisiteSkillTags);
    }

    [Fact]
    public void BuildPath_WhenPrerequisitesWereAlreadyDemonstrated_StartsTheNodeAvailable()
    {
        var gap = new SkillGapAnalyzer(LearningPathTestData.Taxonomy)
            .Analyze(LearningPathTestData.Goal("authentication-jwt"), ["rest-api-design"]);

        var nodes = _builder.BuildPath(gap);

        var node = Assert.Single(nodes);
        Assert.Equal("authentication-jwt", node.SkillTag);
        Assert.Equal(NodeStatus.Available, node.Status);
        Assert.Empty(node.PrerequisiteSkillTags);
    }

    [Fact]
    public void BuildPath_WithAnEmptyGap_ReturnsNoNodes()
    {
        Assert.Empty(_builder.BuildPath(new SkillGap([], [])));
    }

    [Fact]
    public void BuildPath_IsDeterministic()
    {
        var first = _builder.BuildPath(FullGap()).Select(n => n.SkillTag);
        var second = _builder.BuildPath(FullGap()).Select(n => n.SkillTag);

        Assert.Equal(first, second);
    }

    [Fact]
    public void BuildPath_WithACycleInTheTaxonomy_Throws()
    {
        var cyclic = new FakeSkillTaxonomy(new Dictionary<string, string[]> { ["a"] = ["b"], ["b"] = ["a"] });

        Assert.Throws<DomainException>(() => new LearningPathBuilder(cyclic).BuildPath(new SkillGap([], ["a", "b"])));
    }
}