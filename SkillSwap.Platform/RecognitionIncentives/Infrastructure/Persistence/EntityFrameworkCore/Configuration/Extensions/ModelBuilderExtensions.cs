using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.RecognitionIncentives.Infrastructure.Persistence.EntityFrameworkCore.Configuration.
    Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyRecognitionIncentivesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Wallet>(entity =>
        {
            entity.ToTable("wallets",
                table => table.HasCheckConstraint("ck_wallets_balance_non_negative", "balance >= 0"));
            entity.HasKey(w => w.Id);

            entity.Property(w => w.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(w => w.WalletOwnerId).HasColumnName("wallet_owner_id").IsRequired();
            entity.Property(w => w.Balance).HasColumnName("balance").IsRequired();

            // PostgreSQL changes xmin with every update of the row. Using it as a concurrency token makes the
            // second of two simultaneous changes fail instead of overwriting the first one: two redemptions
            // can never spend the same balance.
            entity.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

            // A user has a single wallet (also closes the race between concurrent first credits).
            entity.HasIndex(w => w.WalletOwnerId).IsUnique().HasDatabaseName("ux_wallets_owner");
        });

        builder.Entity<CreditTransaction>(entity =>
        {
            entity.ToTable("credit_transactions",
                table => table.HasCheckConstraint("ck_credit_transactions_amount_positive", "amount > 0"));
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(t => t.WalletId).HasColumnName("wallet_id").IsRequired();

            entity.Property(t => t.Amount).HasColumnName("amount").IsRequired()
                .HasConversion(credits => credits.Value, value => new Credits(value));

            entity.Property(t => t.Type).HasColumnName("type")
                .IsRequired().HasMaxLength(20).HasConversion<string>();
            entity.Property(t => t.Description).HasColumnName("description")
                .IsRequired().HasMaxLength(CreditTransaction.MaxDescriptionLength);
            entity.Property(t => t.RelatedCaseId).HasColumnName("related_case_id");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();

            // The transaction belongs to the wallet of its own bounded context, so this relation is a real foreign
            // key (the case, which belongs to another context, is only an id).
            entity.HasOne<Wallet>().WithMany().HasForeignKey(t => t.WalletId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.WalletId);

            // A case credits a wallet at most once (also closes the race between repeated events).
            entity.HasIndex(t => new { t.WalletId, t.RelatedCaseId }).IsUnique()
                .HasFilter("related_case_id IS NOT NULL")
                .HasDatabaseName("ux_credit_transactions_wallet_case");
        });
    }
}