using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Transform;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Interfaces;

public class ResourceAssemblersTests
{
    // ---------- Redeem command ----------

    [Theory]
    [InlineData("AdvancedPathUnlock", RedemptionItem.AdvancedPathUnlock)]
    [InlineData("advancedpathunlock", RedemptionItem.AdvancedPathUnlock)]
    [InlineData("ContributionCertificate", RedemptionItem.ContributionCertificate)]
    [InlineData("CONTRIBUTIONCERTIFICATE", RedemptionItem.ContributionCertificate)]
    public void Redeem_ParsesTheBenefitIgnoringCase(string item, RedemptionItem expected)
    {
        var command = RedeemCommandFromResourceAssembler.ToCommandFromResource(new RedeemResource(item), 9);

        Assert.Equal(expected, command.Item);
        Assert.Equal(9, command.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("coffee")]
    [InlineData("99")]
    public void Redeem_WithAnUnknownBenefit_ProducesAnUndefinedValue(string? item)
    {
        var command = RedeemCommandFromResourceAssembler.ToCommandFromResource(new RedeemResource(item), 9);

        Assert.False(Enum.IsDefined(command.Item));
    }

    // ---------- Resources ----------

    [Fact]
    public void Wallet_MapsTheOwnerAndTheBalance()
    {
        var wallet = new Wallet(4).Credit(new Credits(35));

        var resource = WalletResourceFromEntityAssembler.ToResourceFromEntity(wallet);

        Assert.Equal(wallet.Id, resource.Id);
        Assert.Equal(4, resource.WalletOwnerId);
        Assert.Equal(35, resource.Balance);
    }

    [Fact]
    public void CreditTransaction_MapsAnEarnedMovementWithItsCase()
    {
        var transaction = new CreditTransaction(2, new Credits(10), TransactionType.Earned,
            "Verification case resolved", 55);

        var resource = CreditTransactionResourceFromEntityAssembler.ToResourceFromEntity(transaction);

        Assert.Equal(transaction.Id, resource.Id);
        Assert.Equal(2, resource.WalletId);
        Assert.Equal(10, resource.Amount);
        Assert.Equal("Earned", resource.Type);
        Assert.Equal("Verification case resolved", resource.Description);
        Assert.Equal(55, resource.RelatedCaseId);
        Assert.Equal(transaction.CreatedAt, resource.CreatedAt);
    }

    [Fact]
    public void CreditTransaction_MapsARedeemedMovementWithoutACase()
    {
        var transaction = new CreditTransaction(2, new Credits(30), TransactionType.Redeemed,
            "Redeemed: contribution certificate");

        var resource = CreditTransactionResourceFromEntityAssembler.ToResourceFromEntity(transaction);

        Assert.Equal("Redeemed", resource.Type);
        Assert.Equal(30, resource.Amount);
        Assert.Null(resource.RelatedCaseId);
    }
}