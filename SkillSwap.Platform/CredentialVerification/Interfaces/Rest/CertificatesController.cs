using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.CredentialVerification.Application.CommandServices;
using SkillSwap.Platform.CredentialVerification.Application.QueryServices;
using SkillSwap.Platform.CredentialVerification.Domain.Model;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Queries;
using SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;
using SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Transform;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using SkillSwap.Platform.Shared.Resources.Errors;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.CredentialVerification.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/certificates")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Certificate endpoints.")]
public class CertificatesController(
    ICertificateCommandService certificateCommandService,
    ICertificateQueryService certificateQueryService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    // The file limit is 10 MB (enforced by the application layer with a friendly error). This larger
    // request limit only stops clearly abusive uploads before they are buffered.
    private const long MaxRequestBytes = 25 * 1024 * 1024;

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Student))]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [SwaggerOperation("Upload Certificate",
        "Upload a certificate file (JPG, PNG or PDF, up to 10 MB) with the fields read by the on-device OCR. " +
        "The owner is the authenticated student. The risk is evaluated and the certificate is registered.",
        OperationId = "UploadCertificate")]
    [SwaggerResponse(201, "The certificate was registered.", typeof(CertificateResource))]
    [SwaggerResponse(400, "The file is missing or a field exceeds its maximum length.")]
    [SwaggerResponse(403, "Only students can upload certificates.")]
    [SwaggerResponse(409, "The student already uploaded this file.")]
    [SwaggerResponse(413, "The file exceeds 10 MB.")]
    [SwaggerResponse(415, "The file is not a JPG, PNG or PDF.")]
    public async Task<IActionResult> UploadCertificate([FromForm] UploadCertificateResource resource,
        CancellationToken cancellationToken)
    {
        var command = await UploadCertificateCommandFromResourceAssembler.ToCommandFromResourceAsync(
            resource, this.CurrentUserId(), cancellationToken);
        var result = await certificateCommandService.Handle(command, cancellationToken);

        return CredentialVerificationActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            certificate => Created(
                $"/api/v1/certificates/{certificate.Id}",
                CertificateResourceFromEntityAssembler.ToResourceFromEntity(
                    certificate, certificateQueryService.GetFileUrl(certificate))));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation("Get Certificate by Id",
        "Get the detail and current verification status of a certificate. Only its owner or a Coordinator can see it.",
        OperationId = "GetCertificateById")]
    [SwaggerResponse(200, "The certificate was found.", typeof(CertificateResource))]
    [SwaggerResponse(403, "The certificate belongs to another student.")]
    [SwaggerResponse(404, "The certificate was not found.")]
    public async Task<IActionResult> GetCertificateById(int id, CancellationToken cancellationToken)
    {
        var certificate = await certificateQueryService.Handle(new GetCertificateByIdQuery(id), cancellationToken);
        if (certificate is null)
            return CredentialVerificationActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, CredentialVerificationError.CertificateNotFound);

        if (!CanAccess(certificate.OwnerId))
            return CredentialVerificationActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, CredentialVerificationError.NotCertificateOwner);

        return Ok(CertificateResourceFromEntityAssembler.ToResourceFromEntity(
            certificate, certificateQueryService.GetFileUrl(certificate)));
    }

    [HttpGet]
    [SwaggerOperation("Get Certificates by Owner",
        "List the certificates of a student, newest first. Without ownerId it lists the authenticated " +
        "student's own certificates. Listing another student's certificates requires the Coordinator role.",
        OperationId = "GetCertificatesByOwner")]
    [SwaggerResponse(200, "The certificates.", typeof(IEnumerable<CertificateResource>))]
    [SwaggerResponse(403, "The certificates belong to another student.")]
    public async Task<IActionResult> GetCertificatesByOwner([FromQuery] int? ownerId,
        CancellationToken cancellationToken)
    {
        var targetId = ownerId ?? this.CurrentUserId();
        if (!CanAccess(targetId))
            return CredentialVerificationActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, CredentialVerificationError.NotCertificateOwner);

        var certificates = await certificateQueryService.Handle(new GetCertificatesByOwnerIdQuery(targetId),
            cancellationToken);

        return Ok(certificates.Select(certificate => CertificateResourceFromEntityAssembler.ToResourceFromEntity(
            certificate, certificateQueryService.GetFileUrl(certificate))));
    }

    /// <summary>
    ///     A certificate can be read by its owner or by a Coordinator.
    /// </summary>
    private bool CanAccess(int ownerId)
    {
        var actor = this.CurrentUser();
        return actor.Id == ownerId || actor.Role == UserRole.Coordinator;
    }
}