using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Reputation.Domain.Model.Aggregates;

/// <summary>
///     StudentEmployabilityScore aggregate root
/// </summary>
/// <remarks>
///     The employability a student demonstrated through the skills they certified, whether the node was approved
///     automatically or by a verifier. The score is recalculated with every certified skill.
/// </remarks>
public class StudentEmployabilityScore
{
    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected StudentEmployabilityScore()
    {
        Score = null!;
    }

    public StudentEmployabilityScore(int studentId)
    {
        if (studentId <= 0)
            throw new DomainException("The score must belong to a valid student.");

        StudentId = studentId;
        Score = new EmployabilityScore(EmployabilityScore.Min);
        UpdatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int VerifiedSkillsCount { get; private set; }
    public EmployabilityScore Score { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    ///     The student certified one more skill.
    /// </summary>
    public StudentEmployabilityScore RecordSkillVerified(IEmployabilityScoreCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(calculator);

        VerifiedSkillsCount++;
        Score = calculator.Calculate(VerifiedSkillsCount);
        UpdatedAt = DateTime.UtcNow;
        return this;
    }
}