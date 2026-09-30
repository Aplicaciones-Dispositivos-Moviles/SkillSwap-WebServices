using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.Property(u => u.Username).HasColumnName("username")
                .IsRequired().HasMaxLength(100)
                .HasConversion(v => v.Value, v => new Username(v));

            entity.Property(u => u.Email).HasColumnName("email")
                .IsRequired().HasMaxLength(255)
                .HasConversion(v => v.Value, v => new Email(v));

            entity.Property(u => u.PasswordHash).HasColumnName("password_hash")
                .IsRequired().HasMaxLength(255)
                .HasConversion(v => v.Value, v => new PasswordHash(v));

            entity.Property(u => u.Role).HasColumnName("role")
                .IsRequired().HasMaxLength(20)
                .HasConversion<string>();

            entity.Property(u => u.IsVerified).HasColumnName("is_verified").IsRequired();

            entity.Property(u => u.Bio).HasColumnName("bio").IsRequired().HasMaxLength(1000);

            entity.Property(u => u.DeviceToken).HasColumnName("device_token")
                .HasMaxLength(512)
                .HasConversion(v => v!.Value, v => new DeviceToken(v));

            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}