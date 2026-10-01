using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.CredentialVerification.Application.ACL;
using SkillSwap.Platform.LearningPathEngine.Application.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Application.Internal.OutboundServices;
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
///     Learning path command service
/// </summary>
/// <param name="learningPathRepository">Learning path repository</param>
/// <param name="taxonomyMatcher">Interprets free text against the skill taxonomy</param>
/// <param name="skillGapAnalyzer">Computes the skill gap</param>
/// <param name="learningPathBuilder">Builds the ordered nodes</param>
/// <param name="credentialContextFacade">Reads the student's certificates from Credential Verification</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class LearningPathCommandService(
    ILearningPathRepository learningPathRepository,
    ISkillTaxonomyMatcher taxonomyMatcher,
    ISkillGapAnalyzer skillGapAnalyzer,
    ILearningPathBuilder learningPathBuilder,
    ICredentialContextFacade credentialContextFacade,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<LearningPathCommandService> logger)
    : ILearningPathCommandService
{
    /// <inheritdoc />
    public async Task<Result<LearningPath>> Handle(DeclareGoalCommand command, CancellationToken cancellationToken)
    {
        var text = command.RawText?.Trim() ?? string.Empty;
        if (text.Length == 0 || text.Length > CareerGoal.MaxRawTextLength)
            return Failure(LearningPathError.InvalidGoal);

        try
        {
            var latest = await learningPathRepository.FindLatestByStudentIdAsync(command.StudentId, cancellationToken);
            if (latest is { Status: PathStatus.Active })
                return Failure(LearningPathError.ActivePathAlreadyExists);

            var skillTags = taxonomyMatcher.Match(text);
            if (skillTags.Count == 0) return Failure(LearningPathError.GoalNotInterpretable);

            var goal = new CareerGoal(text, skillTags);
            var demonstrated = await learningPathRepository.FindCompletedSkillTagsByStudentIdAsync(
                command.StudentId, cancellationToken);
            var gap = skillGapAnalyzer.Analyze(goal, demonstrated);
            if (gap.IsEmpty) return Failure(LearningPathError.GoalAlreadyAchieved);

            var path = new LearningPath(command.StudentId, goal, learningPathBuilder.BuildPath(gap));
            await LinkEvidenceAsync(path, command.StudentId, cancellationToken);

            await learningPathRepository.AddAsync(path, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<LearningPath>.Success(path);
        }
        catch (Exception exception)
        {
            return FailureFrom(exception, "declare the goal of student {StudentId}", command.StudentId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LearningPath>> Handle(CompletePathNodeCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = await learningPathRepository.FindByNodeIdAsync(command.PathNodeId, cancellationToken);
            if (path is null) return Failure(LearningPathError.NodeNotFound);

            var node = path.GetNode(command.PathNodeId)!;
            if (node.Status == NodeStatus.Completed) return Failure(LearningPathError.NodeAlreadyCompleted);
            if (node.Status == NodeStatus.Locked)
                return Failure(LearningPathError.NodeLocked,
                    new Dictionary<string, object> { ["pendingPrerequisites"] = path.PendingPrerequisitesOf(node.Id) });

            path.CompleteNode(node.Id);
            learningPathRepository.Update(path);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<LearningPath>.Success(path);
        }
        catch (Exception exception)
        {
            return FailureFrom(exception, "complete the node {PathNodeId}", command.PathNodeId);
        }
    }

    /// <summary>
    ///     Links the student's certificates to the nodes whose skill their course matches. A certificate is
    ///     only supporting evidence: it never completes a node. It is informational, so a failure here
    ///     must not prevent the path from being created.
    /// </summary>
    private async Task LinkEvidenceAsync(LearningPath path, int studentId, CancellationToken cancellationToken)
    {
        try
        {
            var certificates = await credentialContextFacade.GetEvidenceCertificatesAsync(studentId,
                cancellationToken);
            foreach (var certificate in certificates)
            {
                if (string.IsNullOrWhiteSpace(certificate.CourseName)) continue;
                foreach (var skillTag in taxonomyMatcher.Match(certificate.CourseName))
                    path.LinkCertificateToSkill(skillTag, certificate.Id);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Certificates could not be linked to the new path of student {StudentId}",
                studentId);
        }
    }

    private Result<LearningPath> Failure(LearningPathError error, IReadOnlyDictionary<string, object>? details = null)
    {
        var message = localizer[error.ToString()];
        return details is null
            ? Result<LearningPath>.Failure(error, message)
            : Result<LearningPath>.Failure(error, message, details);
    }

    private Result<LearningPath> FailureFrom(Exception exception, string operation, int id)
    {
        var error = LearningPathErrors.FromException(exception);
        if (error != LearningPathError.OperationCancelled)
            logger.LogError(exception, "Could not " + operation, id);
        return Failure(error);
    }
}