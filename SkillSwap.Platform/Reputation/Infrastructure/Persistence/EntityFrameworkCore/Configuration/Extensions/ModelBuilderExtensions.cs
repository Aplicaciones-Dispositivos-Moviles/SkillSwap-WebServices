using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Reputation.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyReputationConfiguration(this ModelBuilder builder)
    {
        builder.Entity<VerifierReliability>(entity =>
        {
            entity.ToTable("verifier_reliabilities");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(r => r.VerifierUserId).HasColumnName("verifier_user_id").IsRequired();
            entity.Property(r => r.ResolvedCasesCount).HasColumnName("resolved_cases_count").IsRequired();
            entity.Property(r => r.OverturnedDecisionsCount).HasColumnName("overturned_decisions_count")
                .IsRequired();
            entity.Property(r => r.SanctionsCount).HasColumnName("sanctions_count").IsRequired();

            // The score is always derived from the counters; it is stored so it can be read without recalculating.
            entity.Property(r => r.Score).HasColumnName("score").IsRequired()
                .HasConversion(score => score.Value, value => new ReliabilityScore(value));

            entity.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // A verifier has a single reliability record (also closes the race between concurrent first events).
            entity.HasIndex(r => r.VerifierUserId).IsUnique().HasDatabaseName("ux_verifier_reliabilities_user");
        });

        builder.Entity<StudentEmployabilityScore>(entity =>
        {
            entity.ToTable("student_employability_scores");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(s => s.StudentId).HasColumnName("student_id").IsRequired();
            entity.Property(s => s.VerifiedSkillsCount).HasColumnName("verified_skills_count").IsRequired();

            entity.Property(s => s.Score).HasColumnName("score").IsRequired()
                .HasConversion(score => score.Value, value => new EmployabilityScore(value));

            entity.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // A student has a single employability record.
            entity.HasIndex(s => s.StudentId).IsUnique().HasDatabaseName("ux_student_employability_scores_student");
        });
    }
}