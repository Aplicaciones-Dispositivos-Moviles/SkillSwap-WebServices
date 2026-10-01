using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.CredentialVerification.Domain.Repositories;
using SkillSwap.Platform.LearningPathEngine.Application.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Application.QueryServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Integration;

public class LearningPathServicesIntegrationTests : ApiTestBase
{
    private const string RestAndJwt = "quiero aprender a construir APIs REST con autenticación JWT";

    private static async Task<Result<LearningPath>> DeclareAsync(string text, int studentId = 1)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ILearningPathCommandService>()
            .Handle(new DeclareGoalCommand(studentId, text), default);
    }

    private static async Task<LearningPath> PathOfAsync(int studentId = 1)
    {
        using var scope = TestApi.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<ILearningPathQueryService>()
            .Handle(new GetLearningPathByStudentIdQuery(studentId), default))!;
    }

    private static async Task<Result<LearningPath>> CompleteAsync(int nodeId)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ILearningPathCommandService>()
            .Handle(new CompletePathNodeCommand(nodeId), default);
    }

    private static async Task<Result<AssessmentBlueprint>> GenerateAsync(int nodeId, int studentId = 1)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAssessmentBlueprintCommandService>()
            .Handle(new GenerateAssessmentBlueprintCommand(nodeId, studentId), default);
    }

    private static int NodeId(LearningPath path, string skillTag)
    {
        return path.Nodes.Single(n => n.SkillTag == skillTag).Id;
    }

    // ---------- Wiring ----------

    [Fact]
    public void EveryLearningPathServiceIsRegisteredInTheContainer()
    {
        using var scope = TestApi.CreateScope();
        var provider = scope.ServiceProvider;

        Assert.NotNull(provider.GetRequiredService<ILearningPathCommandService>());
        Assert.NotNull(provider.GetRequiredService<IAssessmentBlueprintCommandService>());
        Assert.NotNull(provider.GetRequiredService<ILearningPathQueryService>());
        Assert.NotNull(provider.GetRequiredService<IAssessmentBlueprintQueryService>());
        Assert.NotNull(provider.GetRequiredService<ISkillTaxonomy>());
        Assert.IsType<FakeQuestionGenerationService>(provider.GetRequiredService<IQuestionGenerationService>());
    }

    // ---------- Declaring a goal ----------

    [Fact]
    public async Task Declare_PersistsThePathBuiltFromTheRealCatalog()
    {
        var result = await DeclareAsync(RestAndJwt);

        Assert.True(result.IsSuccess);
        var path = await PathOfAsync();
        Assert.Equal(
            ["networking-basics", "programming-fundamentals", "http-basics", "rest-api-design", "authentication-jwt"],
            path.Nodes.Select(n => n.SkillTag));
        Assert.Equal(
            [NodeStatus.Available, NodeStatus.Available, NodeStatus.Locked, NodeStatus.Locked, NodeStatus.Locked],
            path.Nodes.Select(n => n.Status));
        Assert.Equal(["authentication-jwt", "rest-api-design"], path.CareerGoal.MappedSkillTags.Order());
    }

    [Fact]
    public async Task Declare_WithAGoalNoSkillMatches_IsRejectedAndSavesNothing()
    {
        var result = await DeclareAsync("quiero cocinar pasteles");

        Assert.Equal(LearningPathError.GoalNotInterpretable, result.Error);
        Assert.Null(await PathOfAsync());
    }

    [Fact]
    public async Task Declare_Twice_ReturnsActivePathAlreadyExists()
    {
        await DeclareAsync(RestAndJwt);

        var result = await DeclareAsync("quiero aprender SQL");

        Assert.Equal(LearningPathError.ActivePathAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Declare_LinksAnExistingCertificateToTheNodeOfItsCourse()
    {
        int certificateId;
        using (var scope = TestApi.CreateScope())
        {
            var certificate = new Certificate(1, "file-hash", "ref/file-hash")
                .ApplyExtractedData(null, "Coursera", "REST API fundamentals", null, null, null, null, null, null, null)
                .AssessRisk(new RiskAssessment(0));
            await scope.ServiceProvider.GetRequiredService<ICertificateRepository>().AddAsync(certificate);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
            certificateId = certificate.Id;
        }

        await DeclareAsync(RestAndJwt);

        var path = await PathOfAsync();
        var linked = Assert.Single(path.Nodes, n => n.LinkedCertificateId is not null);
        Assert.Equal("rest-api-design", linked.SkillTag);
        Assert.Equal(certificateId, linked.LinkedCertificateId);
        Assert.Equal(NodeStatus.Locked, linked.Status);
    }

    // ---------- Assessments ----------

    [Fact]
    public async Task GenerateBlueprint_ForAnAvailableNode_PersistsItAndPointsTheNodeToIt()
    {
        await DeclareAsync(RestAndJwt);
        var nodeId = NodeId(await PathOfAsync(), "networking-basics");

        var result = await GenerateAsync(nodeId);

        Assert.True(result.IsSuccess);
        var path = await PathOfAsync();
        Assert.Equal(result.Value!.Id, path.GetNode(nodeId)!.AssessmentBlueprintId);

        using var scope = TestApi.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<IAssessmentBlueprintQueryService>()
            .Handle(new GetAssessmentBlueprintByPathNodeIdQuery(nodeId), default);
        Assert.Equal(5, stored!.Questions.Count);
        Assert.Equal("networking-basics", stored.SkillTag);
    }

    [Fact]
    public async Task GenerateBlueprint_Again_ReplacesThePointerAndKeepsTheHistory()
    {
        await DeclareAsync(RestAndJwt);
        var nodeId = NodeId(await PathOfAsync(), "networking-basics");
        var first = (await GenerateAsync(nodeId)).Value!;

        var second = (await GenerateAsync(nodeId)).Value!;

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(second.Id, (await PathOfAsync()).GetNode(nodeId)!.AssessmentBlueprintId);
    }

    [Fact]
    public async Task GenerateBlueprint_ForALockedNode_ListsThePendingPrerequisites()
    {
        await DeclareAsync(RestAndJwt);
        var nodeId = NodeId(await PathOfAsync(), "rest-api-design");

        var result = await GenerateAsync(nodeId);

        Assert.Equal(LearningPathError.NodeLocked, result.Error);
        Assert.Equal(["http-basics", "programming-fundamentals"],
            Assert.IsAssignableFrom<IEnumerable<string>>(result.Details!["pendingPrerequisites"]));
    }

    [Fact]
    public async Task GenerateBlueprint_ByAnotherStudent_IsRejected()
    {
        await DeclareAsync(RestAndJwt);
        var nodeId = NodeId(await PathOfAsync(), "networking-basics");

        var result = await GenerateAsync(nodeId, studentId: 2);

        Assert.Equal(LearningPathError.NotPathOwner, result.Error);
    }

    // ---------- Completing nodes and reusing what was demonstrated ----------

    [Fact]
    public async Task CompletingTheWholePath_AllowsANewGoalThatReusesWhatWasDemonstrated()
    {
        await DeclareAsync(RestAndJwt);
        foreach (var skill in new[]
                 {
                     "networking-basics", "programming-fundamentals", "http-basics", "rest-api-design",
                     "authentication-jwt"
                 })
            Assert.True((await CompleteAsync(NodeId(await PathOfAsync(), skill))).IsSuccess);
        Assert.Equal(PathStatus.Completed, (await PathOfAsync()).Status);

        var repeated = await DeclareAsync("quiero aprender APIs REST");
        var next = await DeclareAsync("quiero aprender Spring Boot");

        Assert.Equal(LearningPathError.GoalAlreadyAchieved, repeated.Error);
        Assert.True(next.IsSuccess);
        var path = await PathOfAsync();
        Assert.Equal(["oop", "java-language", "spring-boot"], path.Nodes.Select(n => n.SkillTag));
        Assert.Equal([NodeStatus.Available, NodeStatus.Locked, NodeStatus.Locked], path.Nodes.Select(n => n.Status));
    }

    [Fact]
    public async Task CompleteNode_OnALockedNode_IsRejected()
    {
        await DeclareAsync(RestAndJwt);

        var result = await CompleteAsync(NodeId(await PathOfAsync(), "authentication-jwt"));

        Assert.Equal(LearningPathError.NodeLocked, result.Error);
    }
}