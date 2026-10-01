using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Integration;

public class LearningPathPersistenceTests : ApiTestBase
{
    private static async Task SaveAsync(LearningPath path)
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILearningPathRepository>().AddAsync(path);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    private static async Task<LearningPath> LoadAsync(int studentId)
    {
        using var scope = TestApi.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<ILearningPathRepository>()
            .FindLatestByStudentIdAsync(studentId, default))!;
    }

    /// <summary>
    ///     Loads the latest path of a student, applies a change and saves it, as the services do.
    /// </summary>
    private static async Task ChangeAsync(int studentId, Action<LearningPath> change)
    {
        using var scope = TestApi.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILearningPathRepository>();
        var path = (await repository.FindLatestByStudentIdAsync(studentId, default))!;
        change(path);
        repository.Update(path);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    private static int NodeId(LearningPath path, string skillTag)
    {
        return path.Nodes.Single(n => n.SkillTag == skillTag).Id;
    }

    // ---------- Paths ----------

    [Fact]
    public async Task Path_RoundTripsTheGoalTheNodesAndTheirPrerequisites()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(7));

        var path = await LoadAsync(7);

        Assert.True(path.Id > 0);
        Assert.Equal(7, path.StudentId);
        Assert.Equal(PathStatus.Active, path.Status);
        Assert.Equal("I want to build APIs", path.CareerGoal.RawText);
        Assert.Equal(["authentication-jwt"], path.CareerGoal.MappedSkillTags);
        Assert.Equal(DateTimeKind.Utc, path.CreatedAt.Kind);

        Assert.Equal(
            ["networking-basics", "programming-fundamentals", "http-basics", "rest-api-design", "authentication-jwt"],
            path.Nodes.Select(n => n.SkillTag));
        Assert.Equal([1, 2, 3, 4, 5], path.Nodes.Select(n => n.Order));
        Assert.Equal(
            [NodeStatus.Available, NodeStatus.Available, NodeStatus.Locked, NodeStatus.Locked, NodeStatus.Locked],
            path.Nodes.Select(n => n.Status));
        Assert.All(path.Nodes, n => Assert.True(n.Id > 0));
        Assert.Equal(["http-basics", "programming-fundamentals"],
            path.Nodes.Single(n => n.SkillTag == "rest-api-design").PrerequisiteSkillTags);
        Assert.Empty(path.Nodes.Single(n => n.SkillTag == "networking-basics").PrerequisiteSkillTags);
    }

    [Fact]
    public async Task LinkedCertificate_IsPersistedWithoutCompletingTheNode()
    {
        var path = LearningPathTestData.NewUnsavedPath(7);
        path.LinkCertificateToSkill("http-basics", 42);
        await SaveAsync(path);

        var loaded = await LoadAsync(7);

        var node = loaded.Nodes.Single(n => n.SkillTag == "http-basics");
        Assert.Equal(42, node.LinkedCertificateId);
        Assert.Equal(NodeStatus.Locked, node.Status);
    }

    [Fact]
    public async Task BlueprintPointer_IsPersisted()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(7));
        var nodeId = NodeId(await LoadAsync(7), "networking-basics");

        await ChangeAsync(7, path => path.AttachBlueprint(nodeId, 99));

        Assert.Equal(99, (await LoadAsync(7)).GetNode(nodeId)!.AssessmentBlueprintId);
    }

    [Fact]
    public async Task CompletedNodes_ArePersistedAndUnlockTheNextOnes()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(7));
        var loaded = await LoadAsync(7);

        await ChangeAsync(7, path => path.CompleteNode(NodeId(loaded, "networking-basics")));

        var path = await LoadAsync(7);
        Assert.Equal(NodeStatus.Completed, path.Nodes.Single(n => n.SkillTag == "networking-basics").Status);
        Assert.Equal(NodeStatus.Available, path.Nodes.Single(n => n.SkillTag == "http-basics").Status);
        Assert.Equal(NodeStatus.Locked, path.Nodes.Single(n => n.SkillTag == "rest-api-design").Status);
        Assert.True(path.UpdatedAt >= path.CreatedAt);
    }

    // ---------- Queries ----------

    [Fact]
    public async Task FindByNodeId_ReturnsThePathWithAllItsNodes()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(7));
        await SaveAsync(LearningPathTestData.NewUnsavedPath(8));
        var nodeId = NodeId(await LoadAsync(8), "http-basics");

        using var scope = TestApi.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<ILearningPathRepository>()
            .FindByNodeIdAsync(nodeId, default);

        Assert.Equal(8, found!.StudentId);
        Assert.Equal(5, found.Nodes.Count);
    }

    [Fact]
    public async Task FindByNodeId_WithAnUnknownNode_ReturnsNull()
    {
        using var scope = TestApi.CreateScope();

        Assert.Null(await scope.ServiceProvider.GetRequiredService<ILearningPathRepository>()
            .FindByNodeIdAsync(9999, default));
    }

    [Fact]
    public async Task FindCompletedSkillTags_ReturnsOnlyTheCompletedSkillsOfThatStudent()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(1));
        await SaveAsync(LearningPathTestData.NewUnsavedPath(2));
        var first = await LoadAsync(1);
        var second = await LoadAsync(2);
        await ChangeAsync(1, path => path.CompleteNode(NodeId(first, "networking-basics")));
        await ChangeAsync(2, path => path.CompleteNode(NodeId(second, "programming-fundamentals")));

        using var scope = TestApi.CreateScope();
        var tags = await scope.ServiceProvider.GetRequiredService<ILearningPathRepository>()
            .FindCompletedSkillTagsByStudentIdAsync(1, default);

        Assert.Equal(["networking-basics"], tags);
    }

    [Fact]
    public async Task FindLatest_ReturnsTheNewestPathOfTheStudent()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(1, "networking-basics"));
        await ChangeAsync(1, path => path.CompleteNode(path.Nodes[0].Id));
        await SaveAsync(LearningPathTestData.NewUnsavedPath(1, "sql-fundamentals"));

        var latest = await LoadAsync(1);

        Assert.Equal(["sql-fundamentals"], latest.Nodes.Select(n => n.SkillTag));
    }

    // ---------- One active path per student ----------

    [Fact]
    public async Task Database_RejectsASecondActivePathOfTheSameStudent()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(1));

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(LearningPathTestData.NewUnsavedPath(1)));
    }

    [Fact]
    public async Task Database_AllowsActivePathsOfDifferentStudents()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(1));

        await SaveAsync(LearningPathTestData.NewUnsavedPath(2));

        Assert.Equal(2, (await LoadAsync(2)).StudentId);
    }

    [Fact]
    public async Task Database_AllowsANewActivePathOnceThePreviousIsCompleted()
    {
        await SaveAsync(LearningPathTestData.NewUnsavedPath(1, "networking-basics"));
        await ChangeAsync(1, path => path.CompleteNode(path.Nodes[0].Id));
        Assert.Equal(PathStatus.Completed, (await LoadAsync(1)).Status);

        await SaveAsync(LearningPathTestData.NewUnsavedPath(1, "sql-fundamentals"));

        Assert.Equal(PathStatus.Active, (await LoadAsync(1)).Status);
    }

    // ---------- Assessment blueprints ----------

    private static async Task SaveBlueprintAsync(AssessmentBlueprint blueprint)
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IAssessmentBlueprintRepository>().AddAsync(blueprint);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    [Fact]
    public async Task Blueprint_RoundTripsTheQuestionsWithTheirCorrectAnswers()
    {
        var original = new AssessmentBlueprint(7, "http-basics", LearningPathTestData.Questions(5));
        await SaveBlueprintAsync(original);

        using var scope = TestApi.CreateScope();
        var loaded = (await scope.ServiceProvider.GetRequiredService<IAssessmentBlueprintRepository>()
            .FindLatestByPathNodeIdAsync(7, default))!;

        Assert.True(loaded.Id > 0);
        Assert.Equal("http-basics", loaded.SkillTag);
        Assert.Equal(DateTimeKind.Utc, loaded.GeneratedAt.Kind);
        Assert.Equal(original.Questions.Select(q => q.QuestionString), loaded.Questions.Select(q => q.QuestionString));
        Assert.Equal(original.Questions.Select(q => q.CorrectAnswer), loaded.Questions.Select(q => q.CorrectAnswer));
        Assert.Equal(original.Questions[2].Answers, loaded.Questions[2].Answers);
    }

    [Fact]
    public async Task FindLatestBlueprint_ReturnsTheNewestOneOfTheNode()
    {
        await SaveBlueprintAsync(new AssessmentBlueprint(7, "http-basics", LearningPathTestData.Questions(5)));
        var newest = new AssessmentBlueprint(7, "http-basics",
            Enumerable.Range(11, 5).Select(LearningPathTestData.Question).ToList());
        await SaveBlueprintAsync(newest);
        await SaveBlueprintAsync(new AssessmentBlueprint(8, "sql-fundamentals", LearningPathTestData.Questions(5)));

        using var scope = TestApi.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IAssessmentBlueprintRepository>()
            .FindLatestByPathNodeIdAsync(7, default);

        Assert.Equal(newest.Id, found!.Id);
        Assert.Equal("Question 11?", found.Questions[0].QuestionString);
    }

    // ---------- Schema ----------

    [Theory]
    [InlineData("learning_paths", "career_goal")]
    [InlineData("assessment_blueprints", "questions")]
    public async Task StructuredColumns_AreStoredAsJsonb(string table, string column)
    {
        using var scope = TestApi.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var types = await context.Database.SqlQueryRaw<string>(
            "SELECT data_type AS \"Value\" FROM information_schema.columns " +
            $"WHERE table_name = '{table}' AND column_name = '{column}'").ToListAsync();

        Assert.Equal(["jsonb"], types);
    }
}