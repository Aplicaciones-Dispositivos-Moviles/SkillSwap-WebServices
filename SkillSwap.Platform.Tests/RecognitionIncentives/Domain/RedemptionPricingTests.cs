using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Domain;

public class RedemptionPricingTests
{
    private readonly RedemptionPricing _pricing = new();

    [Theory]
    [InlineData(RedemptionItem.AdvancedPathUnlock, 50)]
    [InlineData(RedemptionItem.ContributionCertificate, 30)]
    public void CalculateCost_ReturnsThePriceOfTheBenefit(RedemptionItem item, int expected)
    {
        Assert.Equal(expected, _pricing.CalculateCost(item).Value);
    }

    [Fact]
    public void CalculateCost_PricesEveryBenefitWithAPositiveAmount()
    {
        foreach (var item in Enum.GetValues<RedemptionItem>())
            Assert.True(_pricing.CalculateCost(item).IsPositive, $"{item} has no price.");
    }

    [Fact]
    public void CalculateCost_OfAnUndefinedBenefit_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => _pricing.CalculateCost((RedemptionItem)99));
    }

    [Fact]
    public void CreditRewards_PayTenCreditsPerResolvedCase()
    {
        Assert.Equal(10, CreditRewards.ForResolvedCase().Value);
        Assert.Equal(10, CreditRewards.PerResolvedCase);
    }
}