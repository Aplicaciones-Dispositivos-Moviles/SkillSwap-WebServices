using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.AssessmentPeerReview.Application.ACL;
using SkillSwap.Platform.Reputation.Application.CommandServices;
using SkillSwap.Platform.Reputation.Domain.Model;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Commands;
using SkillSwap.Platform.Reputation.Domain.Repositories;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.Reputation.Application.Internal.CommandServices;

/// <summary>
///     Reputation command service
/// </summary>
/// <param name="reliabilityRepository">Verifier reliability repository</param>
/// <param name="employabilityRepository">Student employability score repository</param>
/// <param name="reliabilityCalculator">Calculates the reliability of a verifier</param>
/// <param name="employabilityCalculator">Calculates the employability of a student</param>
/// <param name="verifierProfileFacade">Stores the reliability as the rating of the verifier profile</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class ReputationCommandService(
    IVerifierReliabilityRepository reliabilityRepository,
    IStudentEmployabilityScoreRepository employabilityRepository,
    IVerifierReliabilityCalculator reliabilityCalculator,
    IEmployabilityScoreCalculator employabilityCalculator,
    IVerifierProfileContextFacade verifierProfileFacade,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<ReputationCommandService> logger)
    : IReputationCommandService
{
    /// <inheritdoc />
    public async Task<Result<VerifierReliability>> Handle(RecordCaseResolutionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var reliability = await reliabilityRepository.FindByVerifierUserIdAsync(command.VerifierUserId,
                cancellationToken);
            var isNewReliability = reliability is null;
            reliability ??= new VerifierReliability(command.VerifierUserId);
            reliability.RecordResolution(reliabilityCalculator);

            StudentEmployabilityScore? employability = null;
            var isNewEmployability = false;
            if (command.Approved)
            {
                employability = await employabilityRepository.FindByStudentIdAsync(command.StudentId,
                    cancellationToken);
                isNewEmployability = employability is null;
                employability ??= new StudentEmployabilityScore(command.StudentId);
                employability.RecordSkillVerified(employabilityCalculator);
            }

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (isNewReliability) await reliabilityRepository.AddAsync(reliability, cancellationToken);
                else reliabilityRepository.Update(reliability);

                if (employability is not null)
                {
                    if (isNewEmployability) await employabilityRepository.AddAsync(employability, cancellationToken);
                    else employabilityRepository.Update(employability);
                }

                await unitOfWork.CompleteAsync(cancellationToken);
            }, cancellationToken);

            await SyncRatingAsync(reliability, cancellationToken);
            return Result<VerifierReliability>.Success(reliability);
        }
        catch (Exception exception)
        {
            return FailureFrom<VerifierReliability>(exception,
                "record the resolution of verifier {VerifierUserId}", command.VerifierUserId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<StudentEmployabilityScore>> Handle(RecordAutomaticApprovalCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var employability = await employabilityRepository.FindByStudentIdAsync(command.StudentId,
                cancellationToken);
            var isNew = employability is null;
            employability ??= new StudentEmployabilityScore(command.StudentId);
            employability.RecordSkillVerified(employabilityCalculator);

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (isNew) await employabilityRepository.AddAsync(employability, cancellationToken);
                else employabilityRepository.Update(employability);
                await unitOfWork.CompleteAsync(cancellationToken);
            }, cancellationToken);

            return Result<StudentEmployabilityScore>.Success(employability);
        }
        catch (Exception exception)
        {
            return FailureFrom<StudentEmployabilityScore>(exception,
                "record the automatic approval of student {StudentId}", command.StudentId);
        }
    }

    /// <summary>
    ///     Best effort: the reputation is already saved, so a failure here is only logged.
    /// </summary>
    private async Task SyncRatingAsync(VerifierReliability reliability, CancellationToken cancellationToken)
    {
        try
        {
            await verifierProfileFacade.UpdateRatingAsync(reliability.VerifierUserId, reliability.Score.Value,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not sync the rating of verifier {VerifierUserId}",
                reliability.VerifierUserId);
        }
    }

    private Result<T> Failure<T>(ReputationError error)
    {
        return Result<T>.Failure(error, localizer[error.ToString()]);
    }

    private Result<T> FailureFrom<T>(Exception exception, string operation, int id)
    {
        var error = ReputationErrors.FromException(exception);
        if (error != ReputationError.OperationCancelled)
            logger.LogError(exception, "Could not " + operation, id);
        return Failure<T>(error);
    }
}