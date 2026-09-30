using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SkillSwap.Platform.Iam.Application.CommandServices;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;
using SkillSwap.Platform.Iam.Interfaces.Rest.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.Iam.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/authentication")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Authentication endpoints.")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [SwaggerOperation("Sign Up", "Register a new Student account with an institutional (.edu.pe) email.",
        OperationId = "SignUp")]
    [SwaggerResponse(201, "The account was created.", typeof(UserResource))]
    [SwaggerResponse(400, "Invalid username, email or password.")]
    [SwaggerResponse(409, "The username or email is already taken.")]
    public async Task<IActionResult> SignUp(SignUpResource resource, CancellationToken cancellationToken)
    {
        // Self-registration can only create Student accounts: the role is never taken from the request,
        // so a client cannot elevate its own role. Coordinator accounts are created directly in the database.
        var command = SignUpCommandFromResourceAssembler.ToCommandFromResource(resource, UserRole.Student);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            user => Created($"/api/v1/users/{user.Id}", UserResourceFromEntityAssembler.ToResourceFromEntity(user)));
    }

    [HttpPost("sign-in")]
    [AllowAnonymous]
    [SwaggerOperation("Sign In", "Authenticate with username and password and obtain a JWT.",
        OperationId = "SignIn")]
    [SwaggerResponse(200, "The user was authenticated.", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(401, "Invalid username or password.")]
    public async Task<IActionResult> SignIn(SignInResource resource, CancellationToken cancellationToken)
    {
        var command = SignInCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            authenticated => Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(
                authenticated.User, authenticated.Token)));
    }
}