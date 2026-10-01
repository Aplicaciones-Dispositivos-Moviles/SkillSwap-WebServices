using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.LearningPathEngine.Application.ACL;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal.CommandServices;

/// <summary>
///     Verifier profile command service
/// </summary>
/// <param name="profileRepository">Verifier profile repository</param>
/// <param name="learningPathFacade">Tells whether the student completed a skill</param>
/// <param name="caseAssignmentService">Assigns the pending cases a new or returning verifier can take</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class VerifierProfileCommandService(
    IVerifierProfileRepository profileRepository,
    ILearningPathContextFacade learningPathFacade,
    ICaseAssignmentService caseAssignmentService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<VerifierProfileCommandService> logger)
    : IVerifierProfileCommandService
{
    /// <inheritdoc />
    public async Task<Result<VerifierProfile>> Handle(CreateVerifierProfileCommand command,
        CancellationToken cancellationToken)
    {
        var skillTag = command.SkillTag?.Trim() ?? string.Empty;
        if (skillTag.Length == 0) return Failure(AssessmentPeerReviewError.InvalidSkillTag);

        try
        {
            // The student can only review the skills they demonstrated themselves.
            var completed = await learningPathFacade.HasCompletedSkillAsync(command.UserId, skillTag,
                cancellationToken);
            if (!completed) return Failure(AssessmentPeerReviewError.SkillNotCompleted);

            var existing = await profileRepository.FindByUserIdAsync(command.UserId, cancellationToken);
            if (existing is { Verified: false }) return Failure(AssessmentPeerReviewError.NotAVerifier);
            if (existing is not null && !existing.AddSkill(skillTag))
                return Failure(AssessmentPeerReviewError.VerifierSkillAlreadyEnabled);

            var isNew = existing is null;
            var profile = existing ?? new VerifierProfile(command.UserId, skillTag);

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (isNew) await profileRepository.AddAsync(profile, cancellationToken);
                else profileRepository.Update(profile);
                await unitOfWork.CompleteAsync(cancellationToken);

                await caseAssignmentService.AssignPendingAsync([skillTag], cancellationToken);
            }, cancellationToken);

            return Result<VerifierProfile>.Success(profile);
        }
        catch (Exception exception)
        {
            return FailureFrom(exception, "create the verifier profile of user {UserId}", command.UserId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<VerifierProfile>> Handle(UpdateVerifierAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository.FindByUserIdAsync(command.UserId, cancellationToken);
            if (profile is null || !profile.Verified) return Failure(AssessmentPeerReviewError.NotAVerifier);

            profile.SetAvailability(command.Available);

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                profileRepository.Update(profile);
                await unitOfWork.CompleteAsync(cancellationToken);

                // Turning availability on puts the verifier back in the queue; cases already assigned stay.
                if (command.Available)
                    await caseAssignmentService.AssignPendingAsync(profile.SkillTags, cancellationToken);
            }, cancellationToken);

            return Result<VerifierProfile>.Success(profile);
        }
        catch (Exception exception)
        {
            return FailureFrom(exception, "update the availability of user {UserId}", command.UserId);
        }
    }

    private Result<VerifierProfile> Failure(AssessmentPeerReviewError error)
    {
        return Result<VerifierProfile>.Failure(error, localizer[error.ToString()]);
    }

    private Result<VerifierProfile> FailureFrom(Exception exception, string operation, int id)
    {
        var error = AssessmentPeerReviewErrors.FromException(exception);
        if (error != AssessmentPeerReviewError.OperationCancelled)
            logger.LogError(exception, "Could not " + operation, id);
        return Failure(error);
    }
}