using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.AssessmentPeerReview.Infrastructure.Persistence.EntityFrameworkCore.Configuration.
    Extensions;

public static class ModelBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void ApplyAssessmentPeerReviewConfiguration(this ModelBuilder builder)
    {
        builder.Entity<AssessmentAttempt>(entity =>
        {
            entity.ToTable("assessment_attempts");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(a => a.BlueprintId).HasColumnName("blueprint_id").IsRequired();
            entity.Property(a => a.StudentId).HasColumnName("student_id").IsRequired();

            entity.Property(a => a.SelectedAnswers).HasColumnName("selected_answers")
                .IsRequired().HasColumnType("jsonb")
                .HasConversion(answers => SerializeAnswers(answers), json => DeserializeAnswers(json),
                    ListComparer<int>());

            // Stored as "correct/total"; the value object validates it again when it is read.
            entity.Property(a => a.Score).HasColumnName("score")
                .IsRequired().HasMaxLength(10)
                .HasConversion(score => FormatScore(score), text => ParseScore(text));

            entity.Property(a => a.Passed).HasColumnName("passed").IsRequired();
            entity.Property(a => a.CompletedAt).HasColumnName("completed_at").IsRequired();

            // One attempt per blueprint (also closes the race between concurrent submissions).
            entity.HasIndex(a => a.BlueprintId).IsUnique().HasDatabaseName("ux_assessment_attempts_blueprint");
            entity.HasIndex(a => a.StudentId);
        });

        builder.Entity<VerifierProfile>(entity =>
        {
            entity.ToTable("verifier_profiles");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(p => p.VerifierUserId).HasColumnName("verifier_user_id").IsRequired();

            entity.Property(p => p.SkillTags).HasColumnName("skill_tags")
                .IsRequired().HasColumnType("jsonb")
                .HasConversion(tags => SerializeTags(tags), json => DeserializeTags(json), ListComparer<string>());

            entity.Property(p => p.Available).HasColumnName("available").IsRequired();
            entity.Property(p => p.Verified).HasColumnName("verified").IsRequired();
            entity.Property(p => p.Rating).HasColumnName("rating").IsRequired();
            entity.Property(p => p.ReviewCount).HasColumnName("review_count").IsRequired();
            entity.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();

            // A user has a single verifier profile.
            entity.HasIndex(p => p.VerifierUserId).IsUnique().HasDatabaseName("ux_verifier_profiles_user");
        });

        builder.Entity<VerificationCase>(entity =>
        {
            entity.ToTable("verification_cases");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(c => c.AttemptId).HasColumnName("attempt_id").IsRequired();
            entity.Property(c => c.StudentId).HasColumnName("student_id").IsRequired();
            entity.Property(c => c.VerifierUserId).HasColumnName("verifier_user_id");
            entity.Property(c => c.PathNodeId).HasColumnName("path_node_id").IsRequired();
            entity.Property(c => c.SkillTag).HasColumnName("skill_tag").IsRequired().HasMaxLength(100);

            entity.Property(c => c.Status).HasColumnName("status")
                .IsRequired().HasMaxLength(20).HasConversion<string>();
            entity.Property(c => c.Decision).HasColumnName("decision")
                .HasMaxLength(20).HasConversion<string>();

            entity.Property(c => c.RubricNotes).HasColumnName("rubric_notes")
                .HasMaxLength(VerificationCase.MaxRubricNotesLength);
            entity.Property(c => c.EvidenceUrl).HasColumnName("evidence_url")
                .HasMaxLength(VerificationCase.MaxEvidenceUrlLength);

            entity.Property(c => c.OpenedAt).HasColumnName("opened_at").IsRequired();
            entity.Property(c => c.AssignedAt).HasColumnName("assigned_at");
            entity.Property(c => c.ResolvedAt).HasColumnName("resolved_at");

            // An attempt opens at most one case.
            entity.HasIndex(c => c.AttemptId).IsUnique().HasDatabaseName("ux_verification_cases_attempt");
            // A student has at most one unresolved case per node (also closes the race between requests).
            entity.HasIndex(c => new { c.StudentId, c.PathNodeId }).IsUnique()
                .HasFilter("status <> 'Resolved'")
                .HasDatabaseName("ux_verification_cases_one_open_per_student_node");
            entity.HasIndex(c => c.VerifierUserId);
            entity.HasIndex(c => new { c.SkillTag, c.Status });
        });
    }

    private static ValueComparer<IReadOnlyList<T>> ListComparer<T>()
    {
        return new ValueComparer<IReadOnlyList<T>>(
            (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            list => list.ToList());
    }

    private static string SerializeAnswers(IReadOnlyList<int> answers)
    {
        return JsonSerializer.Serialize(answers.ToList(), JsonOptions);
    }

    private static IReadOnlyList<int> DeserializeAnswers(string json)
    {
        return JsonSerializer.Deserialize<List<int>>(json, JsonOptions)!;
    }

    private static string SerializeTags(IReadOnlyList<string> tags)
    {
        return JsonSerializer.Serialize(tags.ToList(), JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeTags(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions)!;
    }

    private static string FormatScore(Score score)
    {
        return $"{score.Value}/{score.Total}";
    }

    private static Score ParseScore(string text)
    {
        var parts = text.Split('/');
        return new Score(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}