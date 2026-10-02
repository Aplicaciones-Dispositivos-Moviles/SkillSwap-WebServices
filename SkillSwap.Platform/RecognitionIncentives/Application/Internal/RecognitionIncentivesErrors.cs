using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model;

namespace SkillSwap.Platform.RecognitionIncentives.Application.Internal;

internal static class RecognitionIncentivesErrors
{
    public static RecognitionIncentivesError FromException(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => RecognitionIncentivesError.OperationCancelled,
            DbUpdateConcurrencyException => RecognitionIncentivesError.ConcurrentUpdate,
            DbUpdateException => RecognitionIncentivesError.DatabaseError,
            _ => RecognitionIncentivesError.InternalServerError
        };
    }
}