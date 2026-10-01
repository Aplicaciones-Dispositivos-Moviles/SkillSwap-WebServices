using SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.LearningPathEngine.Application.ACL;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal.QueryServices;

/// <summary>
///     Verification case query service
/// </summary>
/// <param name="caseRepository">Verification case repository</param>
/// <param name="attemptRepository">Assessment attempt repository</param>
/// <param name="learningPathFacade">Reads the questions of the blueprint</param>
public class VerificationCaseQueryService(
    IVerificationCaseRepository caseRepository,
    IAssessmentAttemptRepository attemptRepository,
    ILearningPathContextFacade learningPathFacade)
    : IVerificationCaseQueryService
{
    /// <inheritdoc />
    public async Task<VerificationCase?> Handle(GetVerificationCaseByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await caseRepository.FindByIdAsync(query.CaseId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VerificationCaseDetail?> Handle(GetVerificationCaseDetailQuery query,
        CancellationToken cancellationToken)
    {
        var verificationCase = await caseRepository.FindByIdAsync(query.CaseId, cancellationToken);
        if (verificationCase is null) return null;

        var attempt = await attemptRepository.FindByIdAsync(verificationCase.AttemptId, cancellationToken);
        if (attempt is null) return null;

        IReadOnlyList<FailedQuestion> failed = Array.Empty<FailedQuestion>();
        var blueprint = await learningPathFacade.GetBlueprintAsync(attempt.BlueprintId, cancellationToken);
        if (blueprint is not null)
        {
            // The correct answers only identify the failed questions; they are never copied to the result.
            var incorrect = attempt.IncorrectQuestionIndexes(blueprint.Questions.Select(q => q.CorrectAnswer).ToList());
            failed = incorrect
                .Select(index => new FailedQuestion(index + 1, blueprint.Questions[index].Text,
                    blueprint.Questions[index].Answers, attempt.SelectedAnswers[index]))
                .ToList();
        }

        return new VerificationCaseDetail(verificationCase, attempt, failed);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VerificationCase>> Handle(GetVerificationCasesByVerifierQuery query,
        CancellationToken cancellationToken)
    {
        return await caseRepository.FindByVerifierUserIdAsync(query.VerifierUserId, cancellationToken);
    }
}