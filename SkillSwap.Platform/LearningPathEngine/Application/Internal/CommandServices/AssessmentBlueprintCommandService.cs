using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.LearningPathEngine.Application.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.LearningPathEngine.Application.Internal.CommandServices;

/// <summary>
///     Assessment blueprint command service
/// </summary>
/// <param name="learningPathRepository">Learning path repository</param>
/// <param name="blueprintRepository">Assessment blueprint repository</param>
/// <param name="questionGenerationService">Generates the questions (generative AI behind a port)</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class AssessmentBlueprintCommandService(
    ILearningPathRepository learningPathRepository,
    IAssessmentBlueprintRepository blueprintRepository,
    IQuestionGenerationService questionGenerationService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<AssessmentBlueprintCommandService> logger)
    : IAssessmentBlueprintCommandService
{
    /// <inheritdoc />
    public async Task<Result<AssessmentBlueprint>> Handle(GenerateAssessmentBlueprintCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = await learningPathRepository.FindByNodeIdAsync(command.PathNodeId, cancellationToken);
            if (path is null) return Failure(LearningPathError.NodeNotFound);

            // Only the owner of the path can request (and therefore see) the questions of a node.
            if (path.StudentId != command.StudentId) return Failure(LearningPathError.NotPathOwner);

            var node = path.GetNode(command.PathNodeId)!;
            if (node.Status == NodeStatus.Completed) return Failure(LearningPathError.NodeAlreadyCompleted);
            if (node.Status == NodeStatus.Locked)
                return Failure(LearningPathError.NodeLocked,
                    new Dictionary<string, object> { ["pendingPrerequisites"] = path.PendingPrerequisitesOf(node.Id) });

            AssessmentBlueprint blueprint;
            try
            {
                var questions = await questionGenerationService.GenerateQuestionsAsync(node.SkillTag,
                    cancellationToken);
                blueprint = new AssessmentBlueprint(node.Id, node.SkillTag, questions);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Provider failure, or output that does not meet the contract (a DomainException).
                logger.LogError(exception, "Could not generate the assessment of node {PathNodeId} ({SkillTag})",
                    node.Id, node.SkillTag);
                return Failure(LearningPathError.QuestionGenerationFailed);
            }

            // Older blueprints of the node are kept as history; the node points to the latest one.
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await blueprintRepository.AddAsync(blueprint, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);

                path.AttachBlueprint(node.Id, blueprint.Id);
                learningPathRepository.Update(path);
                await unitOfWork.CompleteAsync(cancellationToken);
            }, cancellationToken);

            return Result<AssessmentBlueprint>.Success(blueprint);
        }
        catch (Exception exception)
        {
            var error = LearningPathErrors.FromException(exception);
            if (error != LearningPathError.OperationCancelled)
                logger.LogError(exception, "Could not register the assessment of node {PathNodeId}",
                    command.PathNodeId);
            return Failure(error);
        }
    }

    private Result<AssessmentBlueprint> Failure(LearningPathError error,
        IReadOnlyDictionary<string, object>? details = null)
    {
        var message = localizer[error.ToString()];
        return details is null
            ? Result<AssessmentBlueprint>.Failure(error, message)
            : Result<AssessmentBlueprint>.Failure(error, message, details);
    }
}