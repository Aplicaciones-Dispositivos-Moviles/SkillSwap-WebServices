using System.Net;
using System.Net.Http.Json;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Integration;

public class WalletEndpointsTests : ApiTestBase
{
    private const string WalletsUrl = "/api/v1/wallets";
    private const string RedeemUrl = "/api/v1/credit-transactions/redeem";
    private const string Certificate = "ContributionCertificate";
    private const string Unlock = "AdvancedPathUnlock";

    private static Task<HttpResponseMessage> WalletAsync(SignedInUser actor, int ownerId)
    {
        return actor.Client.GetAsync($"{WalletsUrl}/{ownerId}");
    }

    private static Task<HttpResponseMessage> HistoryAsync(SignedInUser actor, int ownerId)
    {
        return actor.Client.GetAsync($"{WalletsUrl}/{ownerId}/transactions");
    }

    private static Task<HttpResponseMessage> RedeemAsync(SignedInUser user, string? item)
    {
        return user.Client.PostAsJsonAsync(RedeemUrl, new RedeemResource(item));
    }

    private static async Task<int> BalanceOfAsync(SignedInUser user)
    {
        var response = await WalletAsync(user, user.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await AssessmentFlow.ReadAsync<WalletResource>(response)).Balance;
    }

    private static async Task<List<CreditTransactionResource>> HistoryOfAsync(SignedInUser user)
    {
        var response = await HistoryAsync(user, user.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await AssessmentFlow.ReadAsync<List<CreditTransactionResource>>(response);
    }

    // ---------- Wallet ----------

    [Fact]
    public async Task Wallet_AfterSigningUp_IsEmpty()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await WalletAsync(ana, ana.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wallet = await AssessmentFlow.ReadAsync<WalletResource>(response);
        Assert.Equal(ana.Id, wallet.WalletOwnerId);
        Assert.Equal(0, wallet.Balance);
        Assert.True(wallet.Id > 0);
    }

    [Fact]
    public async Task Wallet_AfterResolvingCases_HoldsTenCreditsPerCase()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        await AssessmentFlow.EarnCreditsAsync(bob, 2);

        Assert.Equal(20, await BalanceOfAsync(bob));
    }

    [Fact]
    public async Task Wallet_OfTheStudentWhoseCaseWasResolved_IsNotCredited()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        var (_, attempt) = await AssessmentFlow.FailSkillAsync(ana, "networking-basics");

        await AssessmentFlow.ResolveAsync(bob, attempt.VerificationCaseId!.Value, "Approved", "Meets the rubric.");

        Assert.Equal(0, await BalanceOfAsync(ana));
        Assert.Equal(10, await BalanceOfAsync(bob));
    }

    [Fact]
    public async Task Wallet_ByAnotherUser_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await WalletAsync(bob, ana.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotWalletOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Wallet_ByACoordinator_Returns200()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await WalletAsync(admin, ana.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ana.Id, (await AssessmentFlow.ReadAsync<WalletResource>(response)).WalletOwnerId);
    }

    [Fact]
    public async Task Wallet_OfAnUnknownUser_ReadByACoordinator_Returns404()
    {
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await WalletAsync(admin, 9999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("WalletNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Wallet_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync($"{WalletsUrl}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- History ----------

    [Fact]
    public async Task History_OfANewWallet_IsEmpty()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        Assert.Empty(await HistoryOfAsync(ana));
    }

    [Fact]
    public async Task History_ListsTheMovementsFromTheMostRecent()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 3);
        await RedeemAsync(bob, Certificate);

        var history = await HistoryOfAsync(bob);

        Assert.Equal(["Redeemed", "Earned", "Earned", "Earned"], history.Select(t => t.Type));
        Assert.Equal([30, 10, 10, 10], history.Select(t => t.Amount));
        Assert.Equal(history.OrderByDescending(t => t.Id).Select(t => t.Id), history.Select(t => t.Id));
        Assert.Equal("Redeemed: contribution certificate", history[0].Description);
        Assert.Null(history[0].RelatedCaseId);
        Assert.All(history.Skip(1), t => Assert.NotNull(t.RelatedCaseId));
        Assert.All(history, t => Assert.Equal(DateTimeKind.Utc, t.CreatedAt.Kind));
    }

    [Fact]
    public async Task History_EachCaseAppearsOnce()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 3);

        var cases = (await HistoryOfAsync(bob)).Select(t => t.RelatedCaseId).ToList();

        Assert.Equal(cases.Distinct().Count(), cases.Count);
    }

    [Fact]
    public async Task History_ByAnotherUser_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await HistoryAsync(bob, ana.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotWalletOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task History_ByACoordinator_Returns200()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await HistoryAsync(admin, ana.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task History_OfAnUnknownUser_ReadByACoordinator_Returns404()
    {
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await HistoryAsync(admin, 9999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("WalletNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task History_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync($"{WalletsUrl}/1/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Redeem ----------

    [Fact]
    public async Task Redeem_TheCertificateWithEnoughBalance_Returns201AndTakesThirtyCredits()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 3);

        var response = await RedeemAsync(bob, Certificate);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transaction = await AssessmentFlow.ReadAsync<CreditTransactionResource>(response);
        Assert.Equal("Redeemed", transaction.Type);
        Assert.Equal(30, transaction.Amount);
        Assert.Equal("Redeemed: contribution certificate", transaction.Description);
        Assert.Null(transaction.RelatedCaseId);
        Assert.EndsWith($"/api/v1/wallets/{bob.Id}/transactions", response.Headers.Location!.ToString());
        Assert.Equal(0, await BalanceOfAsync(bob));
    }

    [Fact]
    public async Task Redeem_TheAdvancedPathUnlock_CostsFiftyCredits()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 5);

        var response = await RedeemAsync(bob, Unlock);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(50, (await AssessmentFlow.ReadAsync<CreditTransactionResource>(response)).Amount);
        Assert.Equal(0, await BalanceOfAsync(bob));
    }

    [Fact]
    public async Task Redeem_KeepsTheRemainingBalance()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 4);

        await RedeemAsync(bob, Certificate);

        Assert.Equal(10, await BalanceOfAsync(bob));
    }

    [Fact]
    public async Task Redeem_WithInsufficientBalance_Returns409AndChangesNothing()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 2);

        var response = await RedeemAsync(bob, Certificate);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("InsufficientBalance", await AssessmentFlow.ErrorOf(response));
        Assert.Equal(20, await BalanceOfAsync(bob));
        Assert.Equal(2, (await HistoryOfAsync(bob)).Count);
    }

    [Fact]
    public async Task Redeem_WithAnEmptyWallet_Returns409()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await RedeemAsync(ana, Certificate);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("InsufficientBalance", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Redeem_NeverSpendsTheCreditsOfAnotherUser()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 3);
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await RedeemAsync(ana, Certificate);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(30, await BalanceOfAsync(bob));
    }

    [Fact]
    public async Task Redeem_TwiceInARow_TheSecondOneFailsWhenTheBalanceRunsOut()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 3);

        Assert.Equal(HttpStatusCode.Created, (await RedeemAsync(bob, Certificate)).StatusCode);
        var second = await RedeemAsync(bob, Certificate);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("InsufficientBalance", await AssessmentFlow.ErrorOf(second));
    }

    [Fact]
    public async Task Redeem_TwoAtTheSameTimeWithBalanceForOnlyOne_SpendsItOnce()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.EarnCreditsAsync(bob, 3);

        var responses = await Task.WhenAll(RedeemAsync(bob, Certificate), RedeemAsync(bob, Certificate));

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], responses.Select(r => r.StatusCode).Order());
        var rejected = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Contains(await AssessmentFlow.ErrorOf(rejected), new[] { "InsufficientBalance", "ConcurrentUpdate" });
        Assert.Equal(0, await BalanceOfAsync(bob));
        Assert.Single((await HistoryOfAsync(bob)).Where(t => t.Type == "Redeemed"));
    }

    [Theory]
    [InlineData("coffee")]
    [InlineData("")]
    [InlineData("99")]
    [InlineData(null)]
    public async Task Redeem_WithAnUnknownBenefit_Returns400(string? item)
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await RedeemAsync(ana, item);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidRedemptionItem", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Redeem_AsACoordinator_Returns403()
    {
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await RedeemAsync(admin, Certificate);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Redeem_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().PostAsJsonAsync(RedeemUrl, new RedeemResource(Certificate));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}