using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Reputation.Interfaces.Rest.Transform;

namespace SkillSwap.Platform.Tests.Reputation.Interfaces;

public class ResourceAssemblersTests
{
    [Fact]
    public void VerifierReliability_MapsTheCountersAndTheScore()
    {
        var calculator = new VerifierReliabilityCalculator();
        var reliability = new VerifierReliability(4)
            .RecordResolution(calculator)
            .RecordResolution(calculator)
            .RecordOverturn(calculator)
            .ApplySanction(calculator);

        var resource = VerifierReliabilityResourceFromEntityAssembler.ToResourceFromEntity(reliability);

        Assert.Equal(reliability.Id, resource.Id);
        Assert.Equal(4, resource.VerifierUserId);
        Assert.Equal(2, resource.ResolvedCasesCount);
        Assert.Equal(1, resource.OverturnedDecisionsCount);
        Assert.Equal(1, resource.SanctionsCount);
        Assert.Equal(60, resource.Score);
        Assert.Equal(reliability.UpdatedAt, resource.UpdatedAt);
    }

    [Fact]
    public void StudentEmployability_MapsTheSkillsAndTheScore()
    {
        var calculator = new EmployabilityScoreCalculator();
        var score = new StudentEmployabilityScore(9).RecordSkillVerified(calculator).RecordSkillVerified(calculator);

        var resource = StudentEmployabilityResourceFromEntityAssembler.ToResourceFromEntity(score);

        Assert.Equal(score.Id, resource.Id);
        Assert.Equal(9, resource.StudentId);
        Assert.Equal(2, resource.VerifiedSkillsCount);
        Assert.Equal(20, resource.Score);
        Assert.Equal(score.UpdatedAt, resource.UpdatedAt);
    }
}