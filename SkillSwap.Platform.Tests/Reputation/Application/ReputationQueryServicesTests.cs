using SkillSwap.Platform.Reputation.Application.Internal.QueryServices;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Queries;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Reputation.Application;

public class ReputationQueryServicesTests
{
    [Fact]
    public async Task ReliabilityQuery_ReturnsTheReliabilityOfTheVerifierOrNull()
    {
        var repository = new FakeVerifierReliabilityRepository();
        var reliability = new VerifierReliability(2);
        await repository.AddAsync(reliability);
        var service = new VerifierReliabilityQueryService(repository);

        Assert.Same(reliability,
            await service.Handle(new GetVerifierReliabilityByUserIdQuery(2), CancellationToken.None));
        Assert.Null(await service.Handle(new GetVerifierReliabilityByUserIdQuery(3), CancellationToken.None));
    }

    [Fact]
    public async Task EmployabilityQuery_ReturnsTheScoreOfTheStudentOrNull()
    {
        var repository = new FakeStudentEmployabilityScoreRepository();
        var score = new StudentEmployabilityScore(1);
        await repository.AddAsync(score);
        var service = new StudentEmployabilityQueryService(repository);

        Assert.Same(score,
            await service.Handle(new GetStudentEmployabilityByStudentIdQuery(1), CancellationToken.None));
        Assert.Null(await service.Handle(new GetStudentEmployabilityByStudentIdQuery(2), CancellationToken.None));
    }
}