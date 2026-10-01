using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;

namespace SkillSwap.Platform.LearningPathEngine.Application.Internal;

internal static class LearningPathErrors
{
    public static LearningPathError FromException(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => LearningPathError.OperationCancelled,
            DbUpdateException => LearningPathError.DatabaseError,
            _ => LearningPathError.InternalServerError
        };
    }
}