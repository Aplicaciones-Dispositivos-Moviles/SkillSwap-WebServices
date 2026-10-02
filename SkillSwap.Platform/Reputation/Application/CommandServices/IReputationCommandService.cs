using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.Reputation.Application.CommandServices;

public interface IReputationCommandService
{
    /// <summary>
    ///     Counts a resolved case in the reliability of the verifier and, when it was approved, certifies one more
    ///     skill of the student. Returns the updated reliability.
    /// </summary>
    Task<Result<VerifierReliability>> Handle(RecordCaseResolutionCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Certifies one more skill of a student who passed the assessment without a verifier. Returns the updated
    ///     employability.
    /// </summary>
    Task<Result<StudentEmployabilityScore>> Handle(RecordAutomaticApprovalCommand command,
        CancellationToken cancellationToken);
}