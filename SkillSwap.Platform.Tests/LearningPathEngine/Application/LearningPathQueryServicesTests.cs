using SkillSwap.Platform.LearningPathEngine.Application.Internal.QueryServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Application;

public class LearningPathQueryServicesTests
{
    private readonly FakeAssessmentBlueprintRepository _blueprints = new();
    private readonly FakeLearningPathRepository _paths = new();

    [Fact]
    public async Task GetPath_ReturnsTheLatestPathOfTheStudent()
    {
        var older = LearningPathTestData.NewPath(1);
        var newer = LearningPathTestData.NewPath(1);
        await _paths.AddAsync(older);
        await _paths.AddAsync(newer);
        await _paths.AddAsync(LearningPathTestData.NewPath(2));
        var service = new LearningPathQueryService(_paths);

        var found = await service.Handle(new GetLearningPathByStudentIdQuery(1), CancellationToken.None);

        Assert.Same(newer, found);
    }

    [Fact]
    public async Task GetPath_ForAStudentWithoutPath_ReturnsNull()
    {
        var service = new LearningPathQueryService(_paths);

        Assert.Null(await service.Handle(new GetLearningPathByStudentIdQuery(1), CancellationToken.None));
    }

    [Fact]
    public async Task GetBlueprint_ReturnsTheLatestBlueprintOfTheNode()
    {
        await _blueprints.AddAsync(new AssessmentBlueprint(7, "http-basics", LearningPathTestData.Questions(5)));
        var latest = new AssessmentBlueprint(7, "http-basics", LearningPathTestData.Questions(5));
        await _blueprints.AddAsync(latest);
        await _blueprints.AddAsync(new AssessmentBlueprint(8, "sql-fundamentals", LearningPathTestData.Questions(5)));
        var service = new AssessmentBlueprintQueryService(_blueprints);

        var found = await service.Handle(new GetAssessmentBlueprintByPathNodeIdQuery(7), CancellationToken.None);

        Assert.Same(latest, found);
    }

    [Fact]
    public async Task GetBlueprint_ForANodeWithoutBlueprint_ReturnsNull()
    {
        var service = new AssessmentBlueprintQueryService(_blueprints);

        Assert.Null(await service.Handle(new GetAssessmentBlueprintByPathNodeIdQuery(7), CancellationToken.None));
    }
}