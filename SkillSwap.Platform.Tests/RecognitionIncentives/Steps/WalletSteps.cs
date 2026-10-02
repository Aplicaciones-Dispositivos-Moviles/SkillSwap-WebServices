using System.Net;
using System.Net.Http.Json;
using Reqnroll;
using SkillSwap.Platform.RecognitionIncentives.Domain.Services;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Iam.Steps;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Steps;

/// <summary>
///     Steps for the SkillCredits wallet. The credits are earned by resolving cases, which is done with the steps of
///     Assessment &amp; Peer Review; here the verifier only gets a quantity of credits ready.
/// </summary>
[Binding]
public class WalletSteps(ApiScenarioContext context)
{
    private const string WalletsUrl = "/api/v1/wallets";
    private const string RedeemUrl = "/api/v1/credit-transactions/redeem";

    private SignedInUser User(string username)
    {
        return context.Users[username];
    }

    private Task<HttpResponseMessage> RedeemAsync(string username, string item)
    {
        return User(username).Client.PostAsJsonAsync(RedeemUrl, new RedeemResource(item));
    }

    // ---------- Given ----------

    [Given("{string} has earned {int} SkillCredits by resolving cases")]
    public async Task GivenHasEarnedSkillCredits(string username, int credits)
    {
        Assert.Equal(0, credits % CreditRewards.PerResolvedCase);
        await AssessmentFlow.EarnCreditsAsync(User(username), credits / CreditRewards.PerResolvedCase);
    }

    [Given("{string} has redeemed {string}")]
    public async Task GivenHasRedeemed(string username, string item)
    {
        var response = await RedeemAsync(username, item);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ---------- When ----------

    [When("{string} consults the wallet of {string}")]
    public async Task WhenConsultsTheWalletOf(string actor, string owner)
    {
        context.Response = await User(actor).Client.GetAsync($"{WalletsUrl}/{User(owner).Id}");
    }

    [When("{string} consults the wallet history of {string}")]
    public async Task WhenConsultsTheWalletHistoryOf(string actor, string owner)
    {
        context.Response = await User(actor).Client.GetAsync($"{WalletsUrl}/{User(owner).Id}/transactions");
    }

    [When("{string} redeems {string}")]
    public async Task WhenRedeems(string username, string item)
    {
        context.Response = await RedeemAsync(username, item);
    }

    // ---------- Then ----------

    [Then("the response shows a balance of {int}")]
    public async Task ThenTheResponseShowsABalanceOf(int balance)
    {
        Assert.Equal(balance, (await context.ReadBodyAsync<WalletResource>()).Balance);
    }

    [Then("the wallet of {string} shows a balance of {int}")]
    public async Task ThenTheWalletOfShowsABalanceOf(string owner, int balance)
    {
        var response = await User(owner).Client.GetAsync($"{WalletsUrl}/{User(owner).Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(balance, (await AssessmentFlow.ReadAsync<WalletResource>(response)).Balance);
    }

    [Then("the history lists {int} movements and the most recent is {string} of {int} SkillCredits")]
    public async Task ThenTheHistoryLists(int count, string type, int amount)
    {
        var history = await context.ReadBodyAsync<List<CreditTransactionResource>>();
        Assert.Equal(count, history.Count);
        Assert.Equal(type, history[0].Type);
        Assert.Equal(amount, history[0].Amount);
    }

    [Then("the movement is recorded as {string} of {int} SkillCredits")]
    public async Task ThenTheMovementIsRecorded(string type, int amount)
    {
        var transaction = await context.ReadBodyAsync<CreditTransactionResource>();
        Assert.Equal(type, transaction.Type);
        Assert.Equal(amount, transaction.Amount);
    }
}