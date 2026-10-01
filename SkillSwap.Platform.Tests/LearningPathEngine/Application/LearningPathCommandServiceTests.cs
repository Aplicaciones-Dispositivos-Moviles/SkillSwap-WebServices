using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.CredentialVerification.Application.ACL;
using SkillSwap.Platform.LearningPathEngine.Application.Internal.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Application;

public class LearningPathCommandServiceTests
{
    private const string RestAndJwt = "I want to build REST APIs with JWT";

    private readonly FakeCredentialContextFacade _credentials = new();
    private readonly FakeLearningPathRepository _paths = new();
    private readonly LearningPathCommandService _service;
    private readonly FakeUnitOfWork _unitOfWork = new();

    public LearningPathCommandServiceTests()
    {
        _service = new LearningPathCommandService(
            _paths,
            FakeSkillTaxonomyMatcher.Sample(),
            new SkillGapAnalyzer(LearningPathTestData.Taxonomy),
            new LearningPathBuilder(LearningPathTestData.Taxonomy),
            _credentials,
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<LearningPathCommandService>.Instance);
    }

    private Task<Result<LearningPath>> Declare(string text = RestAndJwt, int studentId = 1)
    {
        return _service.Handle(new DeclareGoalCommand(studentId, text), CancellationToken.None);
    }

    private static void AssertFailure(Result<LearningPath> result, LearningPathError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<LearningPathError>(result.Error));
    }

    private static int NodeId(LearningPath path, string skillTag)
    {
        return path.Nodes.Single(n => n.SkillTag == skillTag).Id;
    }

    /// <summary>
    ///     Declares a goal and completes every node of the resulting path, in order.
    /// </summary>
    private async Task CompleteWholePathAsync(string goalText)
    {
        var path = (await Declare(goalText)).Value!;
        foreach (var node in path.Nodes)
            await _service.Handle(new CompletePathNodeCommand(node.Id), CancellationToken.None);
        Assert.Equal(PathStatus.Completed, path.Status);
    }

    // ---------- Declare goal ----------

    [Fact]
    public async Task Declare_WithAnInterpretableGoal_CreatesTheActivePathInPrerequisiteOrder()
    {
        var result = await Declare();

        Assert.True(result.IsSuccess);
        var path = Assert.Single(_paths.Paths);
        Assert.Equal(PathStatus.Active, path.Status);
        Assert.Equal(1, path.StudentId);
        Assert.Equal(RestAndJwt, path.CareerGoal.RawText);
        Assert.Equal(
            ["networking-basics", "programming-fundamentals", "http-basics", "rest-api-design", "authentication-jwt"],
            path.Nodes.Select(n => n.SkillTag));
        Assert.Equal(
            [NodeStatus.Available, NodeStatus.Available, NodeStatus.Locked, NodeStatus.Locked, NodeStatus.Locked],
            path.Nodes.Select(n => n.Status));
        Assert.Equal(1, _unitOfWork.CompleteCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Declare_WithBlankText_ReturnsInvalidGoal(string text)
    {
        AssertFailure(await Declare(text), LearningPathError.InvalidGoal);
        Assert.Empty(_paths.Paths);
    }

    [Fact]
    public async Task Declare_WithTextOverTheLimit_ReturnsInvalidGoal()
    {
        AssertFailure(await Declare(new string('a', CareerGoal.MaxRawTextLength + 1)), LearningPathError.InvalidGoal);
    }

    [Fact]
    public async Task Declare_WhenNoSkillMatches_ReturnsGoalNotInterpretableAndSavesNothing()
    {
        AssertFailure(await Declare("I want to bake cakes"), LearningPathError.GoalNotInterpretable);
        Assert.Empty(_paths.Paths);
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Declare_WhenTheStudentHasAnActivePath_ReturnsActivePathAlreadyExists()
    {
        await Declare();

        AssertFailure(await Declare("I want to learn SQL"), LearningPathError.ActivePathAlreadyExists);
        Assert.Single(_paths.Paths);
    }

    [Fact]
    public async Task Declare_WhenAnotherStudentHasAnActivePath_IsAllowed()
    {
        await Declare(studentId: 2);

        var result = await Declare(studentId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _paths.Paths.Count);
    }

    [Fact]
    public async Task Declare_AfterCompletingThePreviousPath_SkipsTheSkillsAlreadyDemonstrated()
    {
        await CompleteWholePathAsync("I want to learn HTTP");

        var result = await Declare("I want REST APIs");

        Assert.True(result.IsSuccess);
        Assert.Equal(["programming-fundamentals", "rest-api-design"], result.Value!.Nodes.Select(n => n.SkillTag));
        Assert.Equal([NodeStatus.Available, NodeStatus.Locked], result.Value.Nodes.Select(n => n.Status));
    }

    [Fact]
    public async Task Declare_WhenEveryRequiredSkillWasAlreadyDemonstrated_ReturnsGoalAlreadyAchieved()
    {
        await CompleteWholePathAsync("I want to learn HTTP");

        AssertFailure(await Declare("I want to learn HTTP again"), LearningPathError.GoalAlreadyAchieved);
        Assert.Single(_paths.Paths);
    }

    // ---------- Declare goal: certificates as evidence ----------

    [Fact]
    public async Task Declare_LinksTheCertificatesWhoseCourseMatchesASkillWithoutCompletingTheNode()
    {
        _credentials.Certificates.AddRange(
        [
            new CertificateSummary(10, "Building REST services", "Coursera"),
            new CertificateSummary(11, null, "Udemy"),
            new CertificateSummary(12, "Cooking basics", "Udemy")
        ]);

        var path = (await Declare()).Value!;

        var linked = path.Nodes.Where(n => n.LinkedCertificateId is not null).ToList();
        var node = Assert.Single(linked);
        Assert.Equal("rest-api-design", node.SkillTag);
        Assert.Equal(10, node.LinkedCertificateId);
        Assert.Equal(NodeStatus.Locked, node.Status);
        Assert.DoesNotContain(path.Nodes, n => n.Status == NodeStatus.Completed);
    }

    [Fact]
    public async Task Declare_KeepsTheFirstCertificateWhenSeveralMatchTheSameSkill()
    {
        _credentials.Certificates.AddRange(
        [
            new CertificateSummary(21, "Advanced REST", "Udemy"),
            new CertificateSummary(20, "REST fundamentals", "Coursera")
        ]);

        var path = (await Declare()).Value!;

        Assert.Equal(20, path.Nodes.Single(n => n.SkillTag == "rest-api-design").LinkedCertificateId);
    }

    [Fact]
    public async Task Declare_WhenTheCertificateLookupFails_StillCreatesThePathWithoutLinks()
    {
        _credentials.ExceptionToThrow = new InvalidOperationException("credential context unavailable");

        var result = await Declare();

        Assert.True(result.IsSuccess);
        Assert.All(result.Value!.Nodes, n => Assert.Null(n.LinkedCertificateId));
    }

    // ---------- Declare goal: infrastructure failures ----------

    [Fact]
    public async Task Declare_WhenSavingFails_ReturnsDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await Declare(), LearningPathError.DatabaseError);
    }

    [Fact]
    public async Task Declare_WhenSavingIsCancelled_ReturnsOperationCancelled()
    {
        _unitOfWork.ExceptionToThrow = new OperationCanceledException();

        AssertFailure(await Declare(), LearningPathError.OperationCancelled);
    }

    [Fact]
    public async Task Declare_WhenSomethingUnexpectedFails_ReturnsInternalServerError()
    {
        _unitOfWork.ExceptionToThrow = new InvalidOperationException("boom");

        AssertFailure(await Declare(), LearningPathError.InternalServerError);
    }

    // ---------- Complete node ----------

    [Fact]
    public async Task CompleteNode_OnAnAvailableNode_CompletesItAndUnlocksTheNext()
    {
        var path = (await Declare("I want to learn HTTP")).Value!;

        var result = await _service.Handle(
            new CompletePathNodeCommand(NodeId(path, "networking-basics")), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NodeStatus.Completed, path.Nodes.Single(n => n.SkillTag == "networking-basics").Status);
        Assert.Equal(NodeStatus.Available, path.Nodes.Single(n => n.SkillTag == "http-basics").Status);
        Assert.Equal(PathStatus.Active, path.Status);
    }

    [Fact]
    public async Task CompleteNode_OnTheLastNode_CompletesThePath()
    {
        await CompleteWholePathAsync("I want to learn HTTP");

        var path = _paths.Paths.Single();
        Assert.Equal(PathStatus.Completed, path.Status);
    }

    [Fact]
    public async Task CompleteNode_OnALockedNode_ReturnsNodeLockedListingThePendingPrerequisites()
    {
        var path = (await Declare()).Value!;

        var result = await _service.Handle(
            new CompletePathNodeCommand(NodeId(path, "rest-api-design")), CancellationToken.None);

        AssertFailure(result, LearningPathError.NodeLocked);
        Assert.Equal(["http-basics", "programming-fundamentals"],
            Assert.IsAssignableFrom<IEnumerable<string>>(result.Details!["pendingPrerequisites"]));
        Assert.Equal(NodeStatus.Locked, path.Nodes.Single(n => n.SkillTag == "rest-api-design").Status);
    }

    [Fact]
    public async Task CompleteNode_Twice_ReturnsNodeAlreadyCompleted()
    {
        var path = (await Declare()).Value!;
        var nodeId = NodeId(path, "networking-basics");
        await _service.Handle(new CompletePathNodeCommand(nodeId), CancellationToken.None);

        var result = await _service.Handle(new CompletePathNodeCommand(nodeId), CancellationToken.None);

        AssertFailure(result, LearningPathError.NodeAlreadyCompleted);
    }

    [Fact]
    public async Task CompleteNode_ForAnUnknownNode_ReturnsNodeNotFound()
    {
        var result = await _service.Handle(new CompletePathNodeCommand(99), CancellationToken.None);

        AssertFailure(result, LearningPathError.NodeNotFound);
    }
    
        // ---------- Refresh certificate links ----------

    private Task<Result<LearningPath>> Refresh(int studentId = 1)
    {
        return _service.Handle(new RefreshCertificateLinksCommand(studentId), CancellationToken.None);
    }

    [Fact]
    public async Task Refresh_LinksACertificateUploadedAfterThePathWasCreated()
    {
        await Declare();
        _credentials.Certificates.Add(new CertificateSummary(30, "REST fundamentals", "Coursera"));
        var savesBefore = _unitOfWork.CompleteCalls;

        var result = await Refresh();

        Assert.True(result.IsSuccess);
        var node = result.Value!.Nodes.Single(n => n.SkillTag == "rest-api-design");
        Assert.Equal(30, node.LinkedCertificateId);
        Assert.Equal(NodeStatus.Locked, node.Status);
        Assert.Equal(savesBefore + 1, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Refresh_WithNothingNewToLink_DoesNotSave()
    {
        await Declare();
        var savesBefore = _unitOfWork.CompleteCalls;

        var result = await Refresh();

        Assert.True(result.IsSuccess);
        Assert.Equal(savesBefore, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Refresh_KeepsTheCertificateAlreadyLinked()
    {
        _credentials.Certificates.Add(new CertificateSummary(10, "REST basics", null));
        await Declare();
        _credentials.Certificates.Add(new CertificateSummary(11, "Advanced REST", null));
        var savesBefore = _unitOfWork.CompleteCalls;

        var result = await Refresh();

        Assert.Equal(10, result.Value!.Nodes.Single(n => n.SkillTag == "rest-api-design").LinkedCertificateId);
        Assert.Equal(savesBefore, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Refresh_ForAStudentWithoutPath_ReturnsPathNotFound()
    {
        AssertFailure(await Refresh(), LearningPathError.PathNotFound);
    }

    [Fact]
    public async Task Refresh_OnACompletedPath_ChangesNothing()
    {
        await CompleteWholePathAsync("I want to learn HTTP");
        _credentials.Certificates.Add(new CertificateSummary(30, "HTTP essentials", null));
        var savesBefore = _unitOfWork.CompleteCalls;

        var result = await Refresh();

        Assert.True(result.IsSuccess);
        Assert.All(result.Value!.Nodes, n => Assert.Null(n.LinkedCertificateId));
        Assert.Equal(savesBefore, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Refresh_WhenTheCertificateLookupFails_StillReturnsThePath()
    {
        await Declare();
        _credentials.ExceptionToThrow = new InvalidOperationException("credential context unavailable");

        var result = await Refresh();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Refresh_WhenSavingTheLinksFails_StillReturnsThePath()
    {
        await Declare();
        _credentials.Certificates.Add(new CertificateSummary(30, "REST fundamentals", null));
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        var result = await Refresh();

        Assert.True(result.IsSuccess);
    }
}