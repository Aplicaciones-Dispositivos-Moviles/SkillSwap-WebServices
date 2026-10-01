using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.LearningPathEngine.Application.ACL;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal;

internal static class AssessmentPeerReviewErrors
{
    public static AssessmentPeerReviewError FromException(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => AssessmentPeerReviewError.OperationCancelled,
            DbUpdateException => AssessmentPeerReviewError.DatabaseError,
            _ => AssessmentPeerReviewError.InternalServerError
        };
    }

    public static AssessmentPeerReviewError FromNodeCompletion(NodeCompletionOutcome outcome)
    {
        return outcome == NodeCompletionOutcome.Failed
            ? AssessmentPeerReviewError.DatabaseError
            : AssessmentPeerReviewError.NodeNotAvailable;
    }
}

/// <summary>
///     Thrown inside a transaction when Learning Path Engine could not complete the node, so everything
///     saved so far is rolled back.
/// </summary>
internal sealed class NodeCompletionFailedException(NodeCompletionOutcome outcome)
    : Exception("The node could not be completed.")
{
    public NodeCompletionOutcome Outcome { get; } = outcome;
}