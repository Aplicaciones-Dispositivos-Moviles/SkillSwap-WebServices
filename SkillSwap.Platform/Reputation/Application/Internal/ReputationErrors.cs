using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.Reputation.Domain.Model;

namespace SkillSwap.Platform.Reputation.Application.Internal;

internal static class ReputationErrors
{
    public static ReputationError FromException(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => ReputationError.OperationCancelled,
            DbUpdateException => ReputationError.DatabaseError,
            _ => ReputationError.InternalServerError
        };
    }
}