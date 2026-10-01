using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

/// <summary>
///     VerifierProfile aggregate root
/// </summary>
/// <remarks>
///     Makes a student eligible to review the cases of other students in the skills they demonstrated
///     themselves. It can be switched off by the verifier (availability) or revoked by moderation.
/// </remarks>
public class VerifierProfile
{
    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected VerifierProfile()
    {
        SkillTags = [];
    }

    public VerifierProfile(int verifierUserId, string skillTag)
    {
        if (verifierUserId <= 0)
            throw new DomainException("The profile must belong to a valid user.");

        VerifierUserId = verifierUserId;
        SkillTags = [Normalize(skillTag)];
        Available = true;
        Verified = true;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int VerifierUserId { get; private set; }

    /// <summary>
    ///     The skills the verifier is enabled to review, sorted.
    /// </summary>
    public IReadOnlyList<string> SkillTags { get; private set; }

    public bool Available { get; private set; }

    /// <summary>
    ///     Whether the profile is still enabled; moderation can revoke it.
    /// </summary>
    public bool Verified { get; private set; }

    /// <summary>
    ///     Reliability synchronized from Reputation; zero until it is first calculated.
    /// </summary>
    public double Rating { get; private set; }

    public int ReviewCount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool HasSkill(string skillTag)
    {
        return SkillTags.Contains(skillTag?.Trim() ?? string.Empty, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Whether this verifier can take a case of the skill: still enabled and enabled for that skill.
    /// </summary>
    public bool CanReview(string skillTag)
    {
        return Verified && HasSkill(skillTag);
    }

    /// <summary>
    ///     Enables the verifier for one more skill.
    /// </summary>
    /// <returns>True when the skill was added; false when the verifier already had it.</returns>
    public bool AddSkill(string skillTag)
    {
        var tag = Normalize(skillTag);
        if (SkillTags.Contains(tag, StringComparer.Ordinal)) return false;

        SkillTags = SkillTags.Append(tag).OrderBy(t => t, StringComparer.Ordinal).ToList();
        return true;
    }

    public VerifierProfile SetAvailability(bool available)
    {
        Available = available;
        return this;
    }

    public VerifierProfile IncrementReviewCount()
    {
        ReviewCount++;
        return this;
    }

    public VerifierProfile UpdateRating(double rating)
    {
        if (double.IsNaN(rating) || rating < 0)
            throw new DomainException("The rating cannot be negative.");

        Rating = rating;
        return this;
    }

    public VerifierProfile Revoke()
    {
        Verified = false;
        Available = false;
        return this;
    }

    private static string Normalize(string skillTag)
    {
        if (string.IsNullOrWhiteSpace(skillTag))
            throw new DomainException("The skill tag cannot be empty.");
        return skillTag.Trim();
    }
}