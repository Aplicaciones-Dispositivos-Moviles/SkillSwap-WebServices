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
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal.CommandServices;

/// <summary>
///     Assessment attempt command service
/// </summary>
/// <param name="attemptRepository">Assessment attempt repository</param>
/// <param name="caseRepository">Verification case repository</param>
/// <param name="learningPathFacade">Reads blueprints and completes nodes in Learning Path Engine</param>
/// <param name="caseAssignmentService">Assigns the case that a failed attempt opens</param>
/// <param name="eventPublisher">Publishes domain events once the change is saved</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class AssessmentAttemptCommandService(
    IAssessmentAttemptRepository attemptRepository,
    IVerificationCaseRepository caseRepository,
    ILearningPathContextFacade learningPathFacade,
    ICaseAssignmentService caseAssignmentService,
    IDomainEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<AssessmentAttemptCommandService> logger)
    : IAssessmentAttemptCommandService
{
    /// <inheritdoc />
    public async Task<Result<SubmitAssessmentAttemptOutcome>> Handle(SubmitAssessmentAttemptCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var blueprint = await learningPathFacade.GetBlueprintAsync(command.BlueprintId, cancellationToken);
            if (blueprint is null) return Failure(AssessmentPeerReviewError.BlueprintNotFound);

            // Only the owner of the path answers the assessment.
            if (blueprint.StudentId != command.StudentId)
                return Failure(AssessmentPeerReviewError.NotBlueprintOwner);
            if (!AreValidAnswers(command.SelectedAnswers, blueprint.Questions.Count))
                return Failure(AssessmentPeerReviewError.InvalidAnswers);
            if (!blueprint.NodeIsAvailable) return Failure(AssessmentPeerReviewError.NodeNotAvailable);

            var openCase = await caseRepository.FindOpenByStudentAndNodeAsync(command.StudentId,
                blueprint.PathNodeId, cancellationToken);
            if (openCase is not null) return Failure(AssessmentPeerReviewError.OpenCaseAlreadyExists);

            if (!blueprint.IsLatest) return Failure(AssessmentPeerReviewError.BlueprintOutdated);
            var previous = await attemptRepository.FindByBlueprintIdAsync(blueprint.BlueprintId, cancellationToken);
            if (previous is not null) return Failure(AssessmentPeerReviewError.AttemptAlreadySubmitted);

            var attempt = new AssessmentAttempt(blueprint.BlueprintId, command.StudentId, command.SelectedAnswers,
                blueprint.Questions.Select(q => q.CorrectAnswer).ToList());

            VerificationCase? verificationCase = null;
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await attemptRepository.AddAsync(attempt, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);

                if (attempt.Passed)
                {
                    var outcome = await learningPathFacade.CompleteNodeAsync(blueprint.PathNodeId,
                        cancellationToken);
                    if (outcome != NodeCompletionOutcome.Completed) throw new NodeCompletionFailedException(outcome);
                    return;
                }

                verificationCase = new VerificationCase(attempt.Id, command.StudentId, blueprint.PathNodeId,
                    blueprint.SkillTag);
                await caseAssignmentService.TryAssignAsync(verificationCase, cancellationToken);
                await caseRepository.AddAsync(verificationCase, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
            }, cancellationToken);

            if (attempt.Passed)
                await eventPublisher.PublishAsync(
                    new AssessmentAttemptPassed(attempt.Id, attempt.StudentId, blueprint.PathNodeId,
                        blueprint.SkillTag), cancellationToken);

            return Result<SubmitAssessmentAttemptOutcome>.Success(
                new SubmitAssessmentAttemptOutcome(attempt, verificationCase));
        }
        catch (NodeCompletionFailedException exception)
        {
            return Failure(AssessmentPeerReviewErrors.FromNodeCompletion(exception.Outcome));
        }
        catch (Exception exception)
        {
            var error = AssessmentPeerReviewErrors.FromException(exception);
            if (error != AssessmentPeerReviewError.OperationCancelled)
                logger.LogError(exception, "Could not submit the attempt of student {StudentId} for blueprint " +
                                           "{BlueprintId}", command.StudentId, command.BlueprintId);
            return Failure(error);
        }
    }

    private static bool AreValidAnswers(IReadOnlyList<int>? answers, int questionCount)
    {
        return answers is not null
               && answers.Count == questionCount
               && answers.All(answer => answer >= 0 && answer < AssessmentAttempt.AnswerOptionCount);
    }

    private Result<SubmitAssessmentAttemptOutcome> Failure(AssessmentPeerReviewError error)
    {
        return Result<SubmitAssessmentAttemptOutcome>.Failure(error, localizer[error.ToString()]);
    }
}