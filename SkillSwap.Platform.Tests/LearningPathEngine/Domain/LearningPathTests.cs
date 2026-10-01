using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Domain;

public class LearningPathTests
{
    // Node ids of NewPath(): 1 networking, 2 programming, 3 http, 4 rest, 5 jwt.
    private static LearningPath NewPath()
    {
        return LearningPathTestData.NewPath();
    }

    private static NodeStatus StatusOf(LearningPath path, int nodeId)
    {
        return path.GetNode(nodeId)!.Status;
    }

    // ---------- Creation ----------

    [Fact]
    public void NewPath_StartsActiveWithItsNodesInOrder()
    {
        var path = NewPath();

        Assert.Equal(PathStatus.Active, path.Status);
        Assert.Equal([1, 2, 3, 4, 5], path.Nodes.Select(n => n.Order));
        Assert.Equal(DateTimeKind.Utc, path.CreatedAt.Kind);
        Assert.Equal(path.CreatedAt, path.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithInvalidStudent_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new LearningPath(0, LearningPathTestData.Goal("a"), [new PathNode("a", 1, [])]));
    }

    [Fact]
    public void Constructor_WithoutNodes_Throws()
    {
        Assert.Throws<DomainException>(() => new LearningPath(1, LearningPathTestData.Goal("a"), []));
    }

    [Fact]
    public void Constructor_WithRepeatedOrderOrSkill_Throws()
    {
        var goal = LearningPathTestData.Goal("a");

        Assert.Throws<DomainException>(() =>
            new LearningPath(1, goal, [new PathNode("a", 1, []), new PathNode("b", 1, [])]));
        Assert.Throws<DomainException>(() =>
            new LearningPath(1, goal, [new PathNode("a", 1, []), new PathNode("a", 2, [])]));
    }

    [Fact]
    public void Constructor_WithAPrerequisiteOutsideThePath_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new LearningPath(1, LearningPathTestData.Goal("a"), [new PathNode("a", 1, ["missing"])]));
    }

    // ---------- Completing nodes ----------

    [Fact]
    public void CompleteNode_UnlocksOnlyTheNodesWhosePrerequisitesAreAllCompleted()
    {
        var path = NewPath();

        path.CompleteNode(1);

        Assert.Equal(NodeStatus.Completed, StatusOf(path, 1));
        Assert.Equal(NodeStatus.Available, StatusOf(path, 3));
        Assert.Equal(NodeStatus.Locked, StatusOf(path, 4));
        Assert.Equal(NodeStatus.Locked, StatusOf(path, 5));
    }

    [Fact]
    public void CompleteNode_FollowsTheWholeChainUntilThePathIsCompleted()
    {
        var path = NewPath();

        path.CompleteNode(1).CompleteNode(2);
        Assert.Equal(NodeStatus.Available, StatusOf(path, 3));
        Assert.Equal(NodeStatus.Locked, StatusOf(path, 4));

        path.CompleteNode(3);
        Assert.Equal(NodeStatus.Available, StatusOf(path, 4));
        Assert.Equal(PathStatus.Active, path.Status);

        path.CompleteNode(4);
        Assert.Equal(NodeStatus.Available, StatusOf(path, 5));

        path.CompleteNode(5);
        Assert.Equal(PathStatus.Completed, path.Status);
        Assert.All(path.Nodes, n => Assert.Equal(NodeStatus.Completed, n.Status));
    }

    [Fact]
    public void CompleteNode_OnALockedNode_Throws()
    {
        var path = NewPath();

        Assert.Throws<DomainException>(() => path.CompleteNode(5));
        Assert.Equal(NodeStatus.Locked, StatusOf(path, 5));
    }

    [Fact]
    public void CompleteNode_Twice_Throws()
    {
        var path = NewPath().CompleteNode(1);

        Assert.Throws<DomainException>(() => path.CompleteNode(1));
    }

    [Fact]
    public void CompleteNode_ForANodeOfAnotherPath_Throws()
    {
        Assert.Throws<DomainException>(() => NewPath().CompleteNode(99));
    }

    // ---------- Linking certificates ----------

    [Fact]
    public void LinkCertificate_ToALockedNode_LinksItWithoutCompletingIt()
    {
        var path = NewPath();

        var linked = path.LinkCertificate(5, 42);

        Assert.True(linked);
        Assert.Equal(42, path.GetNode(5)!.LinkedCertificateId);
        Assert.Equal(NodeStatus.Locked, StatusOf(path, 5));
    }

    [Fact]
    public void LinkCertificate_ToAnAvailableNode_DoesNotCompleteIt()
    {
        var path = NewPath();

        path.LinkCertificate(1, 42);

        Assert.Equal(NodeStatus.Available, StatusOf(path, 1));
    }

    [Fact]
    public void LinkCertificate_KeepsTheFirstCertificateLinked()
    {
        var path = NewPath();
        path.LinkCertificate(1, 42);

        var linkedAgain = path.LinkCertificate(1, 43);

        Assert.False(linkedAgain);
        Assert.Equal(42, path.GetNode(1)!.LinkedCertificateId);
    }

    [Fact]
    public void LinkCertificate_ToACompletedNode_ReturnsFalse()
    {
        var path = NewPath().CompleteNode(1);

        Assert.False(path.LinkCertificate(1, 42));
        Assert.Null(path.GetNode(1)!.LinkedCertificateId);
    }

    [Fact]
    public void LinkCertificate_WithInvalidArguments_Throws()
    {
        var path = NewPath();

        Assert.Throws<DomainException>(() => path.LinkCertificate(1, 0));
        Assert.Throws<DomainException>(() => path.LinkCertificate(99, 42));
    }

    // ---------- Attaching assessments ----------

    [Fact]
    public void AttachBlueprint_ToAnAvailableNode_SetsThePointerAndAllowsReplacingIt()
    {
        var path = NewPath();

        path.AttachBlueprint(1, 10);
        Assert.Equal(10, path.GetNode(1)!.AssessmentBlueprintId);

        path.AttachBlueprint(1, 11);
        Assert.Equal(11, path.GetNode(1)!.AssessmentBlueprintId);
    }

    [Fact]
    public void AttachBlueprint_ToALockedOrCompletedNode_Throws()
    {
        var path = NewPath().CompleteNode(1);

        Assert.Throws<DomainException>(() => path.AttachBlueprint(5, 10));
        Assert.Throws<DomainException>(() => path.AttachBlueprint(1, 10));
    }

    [Fact]
    public void AttachBlueprint_WithAnInvalidId_Throws()
    {
        Assert.Throws<DomainException>(() => NewPath().AttachBlueprint(1, 0));
    }
}