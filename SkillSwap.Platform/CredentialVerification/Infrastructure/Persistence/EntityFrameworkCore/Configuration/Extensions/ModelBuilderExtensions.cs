using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.CredentialVerification.Infrastructure.Persistence.EntityFrameworkCore.Configuration.
    Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyCredentialVerificationConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Certificate>(entity =>
        {
            entity.ToTable("certificates");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(c => c.OwnerId).HasColumnName("owner_id").IsRequired();

            entity.Property(c => c.HolderName).HasColumnName("holder_name")
                .HasMaxLength(Certificate.MaxTextLength);
            entity.Property(c => c.InstitutionName).HasColumnName("institution_name")
                .HasMaxLength(Certificate.MaxTextLength);
            entity.Property(c => c.CourseName).HasColumnName("course_name")
                .HasMaxLength(Certificate.MaxTextLength);
            entity.Property(c => c.IssueDate).HasColumnName("issue_date");
            entity.Property(c => c.DurationHours).HasColumnName("duration_hours");
            entity.Property(c => c.CertificateNumber).HasColumnName("certificate_number")
                .HasMaxLength(Certificate.MaxTextLength);
            entity.Property(c => c.VerificationCode).HasColumnName("verification_code")
                .HasMaxLength(Certificate.MaxTextLength);
            entity.Property(c => c.VerificationUrl).HasColumnName("verification_url")
                .HasMaxLength(Certificate.MaxUrlLength);
            entity.Property(c => c.QrPayload).HasColumnName("qr_payload")
                .HasMaxLength(Certificate.MaxQrPayloadLength);
            entity.Property(c => c.OcrText).HasColumnName("ocr_text").IsRequired();

            entity.Property(c => c.FileHash).HasColumnName("file_hash").IsRequired().HasMaxLength(64);
            entity.Property(c => c.StorageReference).HasColumnName("storage_reference")
                .IsRequired().HasMaxLength(512);

            entity.Property(c => c.Status).HasColumnName("status")
                .IsRequired().HasMaxLength(20).HasConversion<string>();
            entity.Property(c => c.VerificationMethod).HasColumnName("verification_method")
                .IsRequired().HasMaxLength(20).HasConversion<string>();

            // Only the score is stored: the risk level is always derived from it by the value object.
            entity.Property(c => c.RiskAssessment).HasColumnName("risk_score")
                .HasConversion(v => v!.Score, v => new RiskAssessment(v));

            entity.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(c => c.VerifiedAt).HasColumnName("verified_at");

            // A user cannot register the same file twice (also closes the race between concurrent uploads).
            entity.HasIndex(c => new { c.OwnerId, c.FileHash }).IsUnique();
            // Support the duplicate-detection lookups.
            entity.HasIndex(c => c.CertificateNumber);
            entity.HasIndex(c => c.VerificationCode);
            entity.HasIndex(c => c.FileHash);
        });
    }
}