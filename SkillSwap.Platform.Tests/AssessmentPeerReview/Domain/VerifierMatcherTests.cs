using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Services;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Domain;

public class VerifierMatcherTests
{
    private const string Skill = "http-basics";

    private readonly VerifierMatcher _matcher = new();

    private static VerifierCandidate Candidate(int userId, int openCases = 0, string skill = Skill,
        bool available = true, bool revoked = false)
    {
        var profile = new VerifierProfile(userId, skill).SetAvailability(available);
        if (revoked) profile.Revoke();
        return new VerifierCandidate(profile, openCases);
    }

    [Fact]
    public void FindVerifier_ChoosesTheOneWithTheFewestOpenCases()
    {
        var chosen = _matcher.FindVerifier(Skill, 1,
            [Candidate(2, openCases: 3), Candidate(3, openCases: 1), Candidate(4, openCases: 2)]);

        Assert.Equal(3, chosen!.VerifierUserId);
    }

    [Fact]
    public void FindVerifier_WithATie_ChoosesTheLowestUserId()
    {
        var chosen = _matcher.FindVerifier(Skill, 1,
            [Candidate(5, openCases: 1), Candidate(2, openCases: 1), Candidate(4, openCases: 1)]);

        Assert.Equal(2, chosen!.VerifierUserId);
    }

    [Fact]
    public void FindVerifier_NeverChoosesTheStudentOfTheCase()
    {
        var chosen = _matcher.FindVerifier(Skill, 2,
            [Candidate(2, openCases: 0), Candidate(3, openCases: 4)]);

        Assert.Equal(3, chosen!.VerifierUserId);
    }

    [Fact]
    public void FindVerifier_SkipsUnavailableRevokedAndUnqualifiedVerifiers()
    {
        var chosen = _matcher.FindVerifier(Skill, 1,
        [
            Candidate(2, available: false),
            Candidate(3, revoked: true),
            Candidate(4, skill: "sql-fundamentals"),
            Candidate(5, openCases: 9)
        ]);

        Assert.Equal(5, chosen!.VerifierUserId);
    }

    [Fact]
    public void FindVerifier_WhenNobodyQualifies_ReturnsNull()
    {
        var chosen = _matcher.FindVerifier(Skill, 1,
            [Candidate(1), Candidate(2, available: false), Candidate(3, skill: "sql-fundamentals")]);

        Assert.Null(chosen);
    }

    [Fact]
    public void FindVerifier_WithoutCandidates_ReturnsNull()
    {
        Assert.Null(_matcher.FindVerifier(Skill, 1, []));
    }
}