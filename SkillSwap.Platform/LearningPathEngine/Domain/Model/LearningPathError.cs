namespace SkillSwap.Platform.LearningPathEngine.Domain.Model;

public enum LearningPathError
{
    None,
    InvalidGoal,
    GoalNotInterpretable,
    GoalAlreadyAchieved,
    ActivePathAlreadyExists,
    PathNotFound,
    NotPathOwner,
    NodeNotFound,
    NodeLocked,
    NodeAlreadyCompleted,
    QuestionGenerationFailed,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}