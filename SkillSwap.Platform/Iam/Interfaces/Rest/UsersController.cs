using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Iam.Application.CommandServices;
using SkillSwap.Platform.Iam.Application.QueryServices;
using SkillSwap.Platform.Iam.Domain.Model.Commands;
using SkillSwap.Platform.Iam.Domain.Model.Queries;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;
using SkillSwap.Platform.Iam.Interfaces.Rest.Transform;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using SkillSwap.Platform.Shared.Resources.Errors;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.Iam.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/users")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available User endpoints.")]
public class UsersController(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet("me")]
    [SwaggerOperation("Get Current User", "Get the full profile of the authenticated user.",
        OperationId = "GetCurrentUser")]
    [SwaggerResponse(200, "The authenticated user's profile.", typeof(UserResource))]
    public IActionResult GetCurrentUser()
    {
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(this.CurrentUser()));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation("Get User by Id",
        "Get a user by its identifier. The account owner and Coordinators receive the full profile " +
        "(UserResource); any other authenticated user receives the public profile (PublicUserResource), " +
        "which excludes the email.",
        OperationId = "GetUserById")]
    [SwaggerResponse(200, "The user was found.", typeof(UserResource))]
    [SwaggerResponse(404, "The user was not found.")]
    public async Task<IActionResult> GetUserById(int id, CancellationToken cancellationToken)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (user is null)
            return IamActionResultAssembler.ToUserNotFoundResult(this, problemDetailsFactory, errorLocalizer);

        var actor = this.CurrentUser();
        var canSeePrivateData = user.Id == actor.Id || actor.Role == UserRole.Coordinator;

        return canSeePrivateData
            ? Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user))
            : Ok(PublicUserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpPatch("{id:int}/bio")]
    [SwaggerOperation("Update User Bio", "Update the authenticated user's profile description.",
        OperationId = "UpdateUserBio")]
    [SwaggerResponse(200, "The bio was updated.", typeof(UserResource))]
    [SwaggerResponse(400, "The bio exceeds the maximum length.")]
    [SwaggerResponse(403, "The authenticated user is not the owner of this profile.")]
    [SwaggerResponse(404, "The user was not found.")]
    public async Task<IActionResult> UpdateUserBio(int id, UpdateUserBioResource resource,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserBioCommand(id, resource.Bio, this.CurrentUserId());
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            user => Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user)));
    }
}