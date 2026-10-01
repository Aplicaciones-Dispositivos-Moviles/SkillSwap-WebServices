using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.LearningPathEngine.Application.ACL;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Events;
using SkillSwap.Platform.Shared.Domain.Exceptions;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal.CommandServices;

/// <summary>
///     Verification case command service
/// </summary>
/// <param name="caseRepository">Verification case repository</param>
/// <param name="profileRepository">Verifier profile repository</param>
/// <param name="learningPathFacade">Completes the node of an approved case</param>
/// <param name="eventPublisher">Publishes domain events once the change is saved</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class VerificationCaseCommandService(
    IVerificationCaseRepository caseRepository,
    IVerifierProfileRepository profileRepository,
    ILearningPathContextFacade learningPathFacade,
    IDomainEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<VerificationCaseCommandService> logger)
    : IVerificationCaseCommandService
{
    /// <inheritdoc />
    public async Task<Result<VerificationCase>> Handle(AttachCaseEvidenceCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var verificationCase = await caseRepository.FindByIdAsync(command.CaseId, cancellationToken);
            if (verificationCase is null) return Failure(AssessmentPeerReviewError.CaseNotFound);
            if (verificationCase.StudentId != command.StudentId)
                return Failure(AssessmentPeerReviewError.NotCaseOwner);
            if (!verificationCase.IsOpen) return Failure(AssessmentPeerReviewError.CaseAlreadyResolved);

            try
            {
                verificationCase.AttachEvidence(command.EvidenceUrl);
            }
            catch (DomainException)
            {
                // The only rule left to break at this point is the format of the link.
                return Failure(AssessmentPeerReviewError.InvalidEvidenceUrl);
            }

            caseRepository.Update(verificationCase);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<VerificationCase>.Success(verificationCase);
        }
        catch (Exception exception)
        {
            return FailureFrom(exception, "attach evidence to the case {CaseId}", command.CaseId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<VerificationCase>> Handle(ResolveVerificationCaseCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.Decision)) return Failure(AssessmentPeerReviewError.InvalidDecision);

        var notes = command.RubricNotes?.Trim() ?? string.Empty;
        if (notes.Length == 0) return Failure(AssessmentPeerReviewError.RubricNotesRequired);
        if (notes.Length > VerificationCase.MaxRubricNotesLength)
            return Failure(AssessmentPeerReviewError.RubricNotesTooLong);

        try
        {
            var verificationCase = await caseRepository.FindByIdAsync(command.CaseId, cancellationToken);
            if (verificationCase is null) return Failure(AssessmentPeerReviewError.CaseNotFound);
            if (!verificationCase.IsAssignedTo(command.VerifierUserId))
                return Failure(AssessmentPeerReviewError.NotAssignedVerifier);
            if (!verificationCase.IsOpen) return Failure(AssessmentPeerReviewError.CaseAlreadyResolved);

            var profile = await profileRepository.FindByUserIdAsync(command.VerifierUserId, cancellationToken);
            if (profile is null || !profile.Verified) return Failure(AssessmentPeerReviewError.NotAVerifier);

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                verificationCase.Resolve(command.Decision, notes);
                profile.IncrementReviewCount();
                caseRepository.Update(verificationCase);
                profileRepository.Update(profile);
                await unitOfWork.CompleteAsync(cancellationToken);

                if (command.Decision != Domain.Model.ValueObjects.ReviewDecision.Approved) return;

                // A node that is already completed is not a problem: the student got there anyway.
                var outcome = await learningPathFacade.CompleteNodeAsync(verificationCase.PathNodeId,
                    cancellationToken);
                if (outcome is not (NodeCompletionOutcome.Completed or NodeCompletionOutcome.AlreadyCompleted))
                    throw new NodeCompletionFailedException(outcome);
            }, cancellationToken);

            await eventPublisher.PublishAsync(
                new VerificationCaseResolved(verificationCase.Id, verificationCase.StudentId,
                    command.VerifierUserId, verificationCase.PathNodeId, verificationCase.SkillTag,
                    command.Decision), cancellationToken);

            return Result<VerificationCase>.Success(verificationCase);
        }
        catch (NodeCompletionFailedException exception)
        {
            return Failure(AssessmentPeerReviewErrors.FromNodeCompletion(exception.Outcome));
        }
        catch (Exception exception)
        {
            return FailureFrom(exception, "resolve the case {CaseId}", command.CaseId);
        }
    }

    private Result<VerificationCase> Failure(AssessmentPeerReviewError error)
    {
        return Result<VerificationCase>.Failure(error, localizer[error.ToString()]);
    }

    private Result<VerificationCase> FailureFrom(Exception exception, string operation, int id)
    {
        var error = AssessmentPeerReviewErrors.FromException(exception);
        if (error != AssessmentPeerReviewError.OperationCancelled)
            logger.LogError(exception, "Could not " + operation, id);
        return Failure(error);
    }
}