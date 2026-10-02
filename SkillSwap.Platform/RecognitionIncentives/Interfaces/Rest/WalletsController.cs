using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.RecognitionIncentives.Application.QueryServices;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Queries;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Transform;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using SkillSwap.Platform.Shared.Resources.Errors;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/wallets")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Wallet endpoints.")]
public class WalletsController(
    IWalletQueryService walletQueryService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet("{userId:int}")]
    [SwaggerOperation("Get Wallet by User Id",
        "Get the SkillCredits balance of a user. The credits are internal and not monetary: they are earned by " +
        "resolving verification cases and can only be spent on benefits of the platform. A wallet is created with " +
        "the account; accounts created before wallets existed get theirs with the first credit. Only the owner " +
        "or a Coordinator can read it.",
        OperationId = "GetWalletByUserId")]
    [SwaggerResponse(200, "The wallet of the user.", typeof(WalletResource))]
    [SwaggerResponse(403, "The wallet belongs to another user.")]
    [SwaggerResponse(404, "The user has no wallet yet.")]
    public async Task<IActionResult> GetWalletByUserId(int userId, CancellationToken cancellationToken)
    {
        var actor = this.CurrentUser();
        if (actor.Id != userId && actor.Role != UserRole.Coordinator)
            return RecognitionIncentivesActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, RecognitionIncentivesError.NotWalletOwner);

        var wallet = await walletQueryService.Handle(new GetWalletByOwnerIdQuery(userId), cancellationToken);
        return wallet is null
            ? RecognitionIncentivesActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, RecognitionIncentivesError.WalletNotFound)
            : Ok(WalletResourceFromEntityAssembler.ToResourceFromEntity(wallet));
    }

    [HttpGet("{userId:int}/transactions")]
    [SwaggerOperation("Get Wallet Transactions by User Id",
        "Get the movements of the wallet of a user, from the most recent to the oldest. Each one states its type " +
        "(Earned or Redeemed), amount and date. Only the owner or a Coordinator can read it.",
        OperationId = "GetWalletTransactionsByUserId")]
    [SwaggerResponse(200, "The movements of the wallet.", typeof(IEnumerable<CreditTransactionResource>))]
    [SwaggerResponse(403, "The wallet belongs to another user.")]
    [SwaggerResponse(404, "The user has no wallet yet.")]
    public async Task<IActionResult> GetWalletTransactionsByUserId(int userId, CancellationToken cancellationToken)
    {
        var actor = this.CurrentUser();
        if (actor.Id != userId && actor.Role != UserRole.Coordinator)
            return RecognitionIncentivesActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, RecognitionIncentivesError.NotWalletOwner);

        var transactions = await walletQueryService.Handle(new GetWalletTransactionsQuery(userId),
            cancellationToken);
        return transactions is null
            ? RecognitionIncentivesActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, RecognitionIncentivesError.WalletNotFound)
            : Ok(transactions.Select(CreditTransactionResourceFromEntityAssembler.ToResourceFromEntity));
    }
}