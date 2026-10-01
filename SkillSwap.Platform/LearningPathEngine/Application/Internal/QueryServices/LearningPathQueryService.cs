using SkillSwap.Platform.LearningPathEngine.Application.QueryServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;

namespace SkillSwap.Platform.LearningPathEngine.Application.Internal.QueryServices;

/// <summary>
///     Learning path query service
/// </summary>
/// <param name="learningPathRepository">Learning path repository</param>
public class LearningPathQueryService(ILearningPathRepository learningPathRepository) : ILearningPathQueryService
{
    /// <inheritdoc />
    public async Task<LearningPath?> Handle(GetLearningPathByStudentIdQuery query,
        CancellationToken cancellationToken)
    {
        return await learningPathRepository.FindLatestByStudentIdAsync(query.StudentId, cancellationToken);
    }
}