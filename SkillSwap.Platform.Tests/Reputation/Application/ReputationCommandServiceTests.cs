using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.Reputation.Application.Internal.CommandServices;
using SkillSwap.Platform.Reputation.Domain.Model;
using SkillSwap.Platform.Reputation.Domain.Model.Commands;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Reputation.Application;

public class ReputationCommandServiceTests
{
    private readonly FakeStudentEmployabilityScoreRepository _employabilities = new();
    private readonly FakeVerifierProfileContextFacade _facade = new();
    private readonly FakeVerifierReliabilityRepository _reliabilities = new();
    private readonly ReputationCommandService _service;
    private readonly FakeUnitOfWork _unitOfWork = new();

    public ReputationCommandServiceTests()
    {
        _service = new ReputationCommandService(
            _reliabilities,
            _employabilities,
            new VerifierReliabilityCalculator(),
            new EmployabilityScoreCalculator(),
            _facade,
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<ReputationCommandService>.Instance);
    }

    private Task<Result<SkillSwap.Platform.Reputation.Domain.Model.Aggregates.VerifierReliability>> Resolve(
        bool approved, int verifierUserId = 2, int studentId = 1)
    {
        return _service.Handle(new RecordCaseResolutionCommand(verifierUserId, studentId, approved),
            CancellationToken.None);
    }

    private static void AssertFailure<T>(Result<T> result, ReputationError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<ReputationError>(result.Error));
    }

    // ---------- Case resolution ----------

    [Fact]
    public async Task Resolution_Approved_CountsTheCaseCertifiesTheSkillAndSyncsTheRating()
    {
        var result = await Resolve(approved: true);

        Assert.True(result.IsSuccess);
        var reliability = Assert.Single(_reliabilities.Items);
        Assert.Same(reliability, result.Value);
        Assert.Equal(2, reliability.VerifierUserId);
        Assert.Equal(1, reliability.ResolvedCasesCount);
        Assert.Equal(100, reliability.Score.Value);

        var employability = Assert.Single(_employabilities.Items);
        Assert.Equal(1, employability.StudentId);
        Assert.Equal(1, employability.VerifiedSkillsCount);
        Assert.Equal(10, employability.Score.Value);

        Assert.Equal((2, 100d), Assert.Single(_facade.Updates));
        Assert.Equal(1, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Resolution_Rejected_CountsTheCaseButCertifiesNothing()
    {
        var result = await Resolve(approved: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, Assert.Single(_reliabilities.Items).ResolvedCasesCount);
        Assert.Empty(_employabilities.Items);
        Assert.Single(_facade.Updates);
    }

    [Fact]
    public async Task Resolution_Repeated_UpdatesTheSameRecordsInsteadOfCreatingNewOnes()
    {
        await Resolve(approved: true, verifierUserId: 2, studentId: 1);
        await Resolve(approved: true, verifierUserId: 2, studentId: 1);
        await Resolve(approved: false, verifierUserId: 2, studentId: 5);

        var reliability = Assert.Single(_reliabilities.Items);
        Assert.Equal(3, reliability.ResolvedCasesCount);
        var employability = Assert.Single(_employabilities.Items);
        Assert.Equal(2, employability.VerifiedSkillsCount);
        Assert.Equal(20, employability.Score.Value);
    }

    [Fact]
    public async Task Resolution_KeepsTheReliabilityOfEachVerifierSeparate()
    {
        await Resolve(approved: true, verifierUserId: 2);
        await Resolve(approved: true, verifierUserId: 3);

        Assert.Equal(2, _reliabilities.Items.Count);
        Assert.All(_reliabilities.Items, r => Assert.Equal(1, r.ResolvedCasesCount));
    }

    [Fact]
    public async Task Resolution_SyncsTheCurrentScoreOfAnExistingReliability()
    {
        var existing = new SkillSwap.Platform.Reputation.Domain.Model.Aggregates.VerifierReliability(2);
        existing.RecordOverturn(new VerifierReliabilityCalculator());
        await _reliabilities.AddAsync(existing);

        await Resolve(approved: false);

        Assert.Equal((2, 85d), Assert.Single(_facade.Updates));
    }

    [Fact]
    public async Task Resolution_ForAUserWithoutAVerifierProfile_StillSucceeds()
    {
        _facade.Updated = false;

        Assert.True((await Resolve(approved: true)).IsSuccess);
    }

    [Fact]
    public async Task Resolution_WhenTheRatingSyncFails_StillSucceedsAndKeepsTheReputation()
    {
        _facade.ExceptionToThrow = new InvalidOperationException("sync failed");

        var result = await Resolve(approved: true);

        Assert.True(result.IsSuccess);
        Assert.Single(_reliabilities.Items);
        Assert.Single(_employabilities.Items);
    }

    [Fact]
    public async Task Resolution_WhenPersistenceFails_FailsWithDatabaseErrorAndDoesNotSync()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await Resolve(approved: true), ReputationError.DatabaseError);
        Assert.Empty(_facade.Updates);
    }

    [Fact]
    public async Task Resolution_WhenTheRequestIsCancelled_FailsWithOperationCancelled()
    {
        _unitOfWork.ExceptionToThrow = new OperationCanceledException();

        AssertFailure(await Resolve(approved: true), ReputationError.OperationCancelled);
    }

    [Fact]
    public async Task Resolution_WithAnInvalidUser_FailsWithInternalServerErrorAndSavesNothing()
    {
        AssertFailure(await Resolve(approved: true, verifierUserId: 0), ReputationError.InternalServerError);
        Assert.Empty(_reliabilities.Items);
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    // ---------- Automatic approval ----------

    [Fact]
    public async Task AutomaticApproval_CertifiesTheSkillOfTheStudent()
    {
        var result = await _service.Handle(new RecordAutomaticApprovalCommand(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var employability = Assert.Single(_employabilities.Items);
        Assert.Same(employability, result.Value);
        Assert.Equal(1, employability.VerifiedSkillsCount);
        Assert.Equal(10, employability.Score.Value);
        Assert.Empty(_reliabilities.Items);
        Assert.Empty(_facade.Updates);
    }

    [Fact]
    public async Task AutomaticApproval_Repeated_AccumulatesOnTheSameRecord()
    {
        await _service.Handle(new RecordAutomaticApprovalCommand(1), CancellationToken.None);
        await _service.Handle(new RecordAutomaticApprovalCommand(1), CancellationToken.None);
        await _service.Handle(new RecordAutomaticApprovalCommand(7), CancellationToken.None);

        Assert.Equal(2, _employabilities.Items.Count);
        Assert.Equal(20, _employabilities.Items.Single(s => s.StudentId == 1).Score.Value);
        Assert.Equal(10, _employabilities.Items.Single(s => s.StudentId == 7).Score.Value);
    }

    [Fact]
    public async Task AutomaticApproval_WhenPersistenceFails_FailsWithDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await _service.Handle(new RecordAutomaticApprovalCommand(1), CancellationToken.None),
            ReputationError.DatabaseError);
    }

    [Fact]
    public async Task AutomaticApproval_WithAnInvalidStudent_FailsWithInternalServerError()
    {
        AssertFailure(await _service.Handle(new RecordAutomaticApprovalCommand(0), CancellationToken.None),
            ReputationError.InternalServerError);
        Assert.Empty(_employabilities.Items);
    }
}