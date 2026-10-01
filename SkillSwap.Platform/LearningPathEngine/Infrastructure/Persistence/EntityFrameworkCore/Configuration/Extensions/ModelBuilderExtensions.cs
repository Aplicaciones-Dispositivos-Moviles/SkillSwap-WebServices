using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.Persistence.EntityFrameworkCore.Configuration.
    Extensions;

public static class ModelBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void ApplyLearningPathEngineConfiguration(this ModelBuilder builder)
    {
        // Questions are stored inside the blueprint's JSON column, never as an entity of their own.
        builder.Ignore<Question>();

        builder.Entity<LearningPath>(entity =>
        {
            entity.ToTable("learning_paths");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(p => p.StudentId).HasColumnName("student_id").IsRequired();

            entity.Property(p => p.CareerGoal).HasColumnName("career_goal")
                .IsRequired().HasColumnType("jsonb")
                .HasConversion(goal => SerializeGoal(goal), json => DeserializeGoal(json));

            entity.Property(p => p.Status).HasColumnName("status")
                .IsRequired().HasMaxLength(20).HasConversion<string>();
            entity.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasMany(p => p.Nodes).WithOne().HasForeignKey("LearningPathId")
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(p => p.Nodes).UsePropertyAccessMode(PropertyAccessMode.Field);

            // A student has at most one active path (also closes the race between concurrent requests).
            entity.HasIndex(p => p.StudentId).IsUnique()
                .HasFilter("status = 'Active'").HasDatabaseName("ux_learning_paths_one_active_per_student");
        });

        builder.Entity<PathNode>(entity =>
        {
            entity.ToTable("path_nodes");
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property<int>("LearningPathId").HasColumnName("learning_path_id");
            entity.Property(n => n.SkillTag).HasColumnName("skill_tag").IsRequired().HasMaxLength(100);
            entity.Property(n => n.Order).HasColumnName("node_order").IsRequired();
            entity.Property(n => n.Status).HasColumnName("status")
                .IsRequired().HasMaxLength(20).HasConversion<string>();

            // Direct prerequisites inside the path, stored as a comma-separated list of kebab-case tags.
            entity.Property(n => n.PrerequisiteSkillTags).HasColumnName("prerequisites")
                .IsRequired().HasMaxLength(1000)
                .HasConversion(tags => JoinTags(tags), text => SplitTags(text), ListComparer<string>());

            entity.Property(n => n.LinkedCertificateId).HasColumnName("linked_certificate_id");
            entity.Property(n => n.AssessmentBlueprintId).HasColumnName("assessment_blueprint_id");

            entity.HasIndex("LearningPathId", nameof(PathNode.SkillTag)).IsUnique();
            entity.HasIndex("LearningPathId", nameof(PathNode.Order)).IsUnique();
        });

        builder.Entity<AssessmentBlueprint>(entity =>
        {
            entity.ToTable("assessment_blueprints");
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(b => b.PathNodeId).HasColumnName("path_node_id").IsRequired();
            entity.Property(b => b.SkillTag).HasColumnName("skill_tag").IsRequired().HasMaxLength(100);

            entity.Property(b => b.Questions).HasColumnName("questions")
                .IsRequired().HasColumnType("jsonb")
                .HasConversion(questions => SerializeQuestions(questions), json => DeserializeQuestions(json),
                    ListComparer<Question>());

            entity.Property(b => b.GeneratedAt).HasColumnName("generated_at").IsRequired();

            entity.HasIndex(b => b.PathNodeId);
        });
    }

    private static ValueComparer<IReadOnlyList<T>> ListComparer<T>()
    {
        return new ValueComparer<IReadOnlyList<T>>(
            (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            list => list.ToList());
    }

    private static string JoinTags(IReadOnlyList<string> tags)
    {
        return string.Join(',', tags);
    }

    private static IReadOnlyList<string> SplitTags(string text)
    {
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string SerializeGoal(CareerGoal goal)
    {
        return JsonSerializer.Serialize(new CareerGoalJson(goal.RawText, goal.MappedSkillTags.ToList()), JsonOptions);
    }

    private static CareerGoal DeserializeGoal(string json)
    {
        var dto = JsonSerializer.Deserialize<CareerGoalJson>(json, JsonOptions)!;
        return new CareerGoal(dto.RawText, dto.SkillTags);
    }

    private static string SerializeQuestions(IReadOnlyList<Question> questions)
    {
        return JsonSerializer.Serialize(
            questions.Select(q => new QuestionJson(q.QuestionString, q.Answers.ToList(), q.CorrectAnswer)).ToList(),
            JsonOptions);
    }

    private static IReadOnlyList<Question> DeserializeQuestions(string json)
    {
        var items = JsonSerializer.Deserialize<List<QuestionJson>>(json, JsonOptions)!;
        return items.Select(item => new Question(item.Question, item.Answers, item.CorrectAnswer)).ToList();
    }

    internal sealed record CareerGoalJson(string RawText, List<string> SkillTags);

    internal sealed record QuestionJson(string Question, List<string> Answers, int CorrectAnswer);
}