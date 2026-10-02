using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.RecognitionIncentives.Application.CommandServices;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Transform;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/credit-transactions")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Credit Transaction endpoints.")]
public class CreditTransactionsController(
    IWalletCommandService walletCommandService,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpPost("redeem")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Redeem a Benefit",
        "Redeem a benefit with SkillCredits: AdvancedPathUnlock costs 50 and ContributionCertificate costs 30. The " +
        "cost is taken from the balance of the authenticated user and the movement is recorded. The mobile app " +
        "asks the user to confirm with the biometrics or the lock screen of the device before calling this " +
        "endpoint; the server does not check it. Delivering the benefit itself is not part of this version.",
        OperationId = "RedeemBenefit")]
    [SwaggerResponse(201, "The benefit was redeemed and the movement recorded.", typeof(CreditTransactionResource))]
    [SwaggerResponse(400, "The benefit is not AdvancedPathUnlock or ContributionCertificate.")]
    [SwaggerResponse(404, "The user has no wallet yet.")]
    [SwaggerResponse(409, "The balance is not enough, or the wallet was modified at the same time.")]
    public async Task<IActionResult> Redeem(RedeemResource resource, CancellationToken cancellationToken)
    {
        var userId = this.CurrentUserId();
        var command = RedeemCommandFromResourceAssembler.ToCommandFromResource(resource, userId);
        var result = await walletCommandService.Handle(command, cancellationToken);

        return RecognitionIncentivesActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            transaction => Created($"/api/v1/wallets/{userId}/transactions",
                CreditTransactionResourceFromEntityAssembler.ToResourceFromEntity(transaction)));
    }
}