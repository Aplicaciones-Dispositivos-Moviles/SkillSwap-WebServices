using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.LearningPathEngine.Application.Internal.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Application;

public class AssessmentBlueprintCommandServiceTests
{
    // Node ids of the seeded path (student 1): 1 networking-basics, 2 programming-fundamentals (available),
    // 3 http-basics, 4 rest-api-design, 5 authentication-jwt (locked).
    private readonly FakeAssessmentBlueprintRepository _blueprints = new();
    private readonly FakeQuestionGenerationService _generator = new();
    private readonly FakeLearningPathRepository _paths = new();
    private readonly AssessmentBlueprintCommandService _service;
    private readonly FakeUnitOfWork _unitOfWork = new();

    public AssessmentBlueprintCommandServiceTests()
    {
        _service = new AssessmentBlueprintCommandService(
            _paths,
            _blueprints,
            _generator,
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<AssessmentBlueprintCommandService>.Instance);
    }

    private async Task<LearningPath> SeedPathAsync(int studentId = 1)
    {
        var path = LearningPathTestData.NewPath(studentId);
        await _paths.AddAsync(path);
        return path;
    }

    private Task<Result<AssessmentBlueprint>> Generate(int nodeId, int studentId = 1)
    {
        return _service.Handle(new GenerateAssessmentBlueprintCommand(nodeId, studentId), CancellationToken.None);
    }

    private static void AssertFailure(Result<AssessmentBlueprint> result, LearningPathError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<LearningPathError>(result.Error));
    }

    // ---------- Happy path ----------

    [Fact]
    public async Task Generate_ForAnAvailableNode_CreatesTheBlueprintAndPointsTheNodeToIt()
    {
        var path = await SeedPathAsync();

        var result = await Generate(1);

        Assert.True(result.IsSuccess);
        var blueprint = result.Value!;
        Assert.Equal(1, blueprint.PathNodeId);
        Assert.Equal("networking-basics", blueprint.SkillTag);
        Assert.Equal(AssessmentBlueprint.QuestionCount, blueprint.Questions.Count);
        Assert.Equal(["networking-basics"], _generator.Requests);
        Assert.Equal(blueprint.Id, path.GetNode(1)!.AssessmentBlueprintId);
        Assert.Equal(2, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Generate_Again_KeepsTheOldBlueprintAndPointsTheNodeToTheNewOne()
    {
        var path = await SeedPathAsync();
        var first = (await Generate(1)).Value!;

        var second = (await Generate(1)).Value!;

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, _blueprints.Blueprints.Count);
        Assert.Equal(second.Id, path.GetNode(1)!.AssessmentBlueprintId);
        Assert.NotEqual(first.Questions.Select(q => q.QuestionString), second.Questions.Select(q => q.QuestionString));
    }

    // ---------- Rejections ----------

    [Fact]
    public async Task Generate_ForALockedNode_ReturnsNodeLockedListingThePendingPrerequisites()
    {
        await SeedPathAsync();

        var result = await Generate(4);

        AssertFailure(result, LearningPathError.NodeLocked);
        Assert.Equal(["http-basics", "programming-fundamentals"],
            Assert.IsAssignableFrom<IEnumerable<string>>(result.Details!["pendingPrerequisites"]));
        Assert.Empty(_generator.Requests);
    }

    [Fact]
    public async Task Generate_ForACompletedNode_ReturnsNodeAlreadyCompleted()
    {
        var path = await SeedPathAsync();
        path.CompleteNode(1);

        AssertFailure(await Generate(1), LearningPathError.NodeAlreadyCompleted);
        Assert.Empty(_generator.Requests);
    }

    [Fact]
    public async Task Generate_ForAnotherStudentsNode_ReturnsNotPathOwnerWithoutGenerating()
    {
        await SeedPathAsync();

        AssertFailure(await Generate(1, studentId: 2), LearningPathError.NotPathOwner);
        Assert.Empty(_generator.Requests);
        Assert.Empty(_blueprints.Blueprints);
    }

    [Fact]
    public async Task Generate_ForAnUnknownNode_ReturnsNodeNotFound()
    {
        AssertFailure(await Generate(99), LearningPathError.NodeNotFound);
    }

    // ---------- Generator failures ----------

    [Fact]
    public async Task Generate_WhenTheProviderFails_ReturnsQuestionGenerationFailedAndSavesNothing()
    {
        await SeedPathAsync();
        _generator.ExceptionToThrow = new HttpRequestException("provider unavailable");

        AssertFailure(await Generate(1), LearningPathError.QuestionGenerationFailed);
        Assert.Empty(_blueprints.Blueprints);
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Generate_WhenTheProviderReturnsTheWrongNumberOfQuestions_ReturnsQuestionGenerationFailed()
    {
        await SeedPathAsync();
        _generator.QuestionCount = 4;

        AssertFailure(await Generate(1), LearningPathError.QuestionGenerationFailed);
        Assert.Empty(_blueprints.Blueprints);
    }

    [Fact]
    public async Task Generate_WhenTheRequestIsCancelledWhileGenerating_ReturnsOperationCancelled()
    {
        await SeedPathAsync();
        _generator.ExceptionToThrow = new OperationCanceledException();

        AssertFailure(await Generate(1), LearningPathError.OperationCancelled);
    }

    // ---------- Persistence failures ----------

    [Fact]
    public async Task Generate_WhenSavingFails_ReturnsDatabaseError()
    {
        await SeedPathAsync();
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await Generate(1), LearningPathError.DatabaseError);
    }
}