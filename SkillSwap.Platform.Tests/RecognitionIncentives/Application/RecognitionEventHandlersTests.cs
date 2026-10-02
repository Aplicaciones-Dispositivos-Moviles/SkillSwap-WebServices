using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Model.Events;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Application.CommandServices;
using SkillSwap.Platform.RecognitionIncentives.Application.EventHandlers;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Application;

public class RecognitionEventHandlersTests
{
    private readonly RecordingWalletCommandService _service = new();

    [Fact]
    public async Task UserRegistered_BecomesACreateWalletCommand()
    {
        var handler = new CreateWalletEventHandler(_service, NullLogger<CreateWalletEventHandler>.Instance);

        await handler.HandleAsync(new UserRegistered(5, UserRole.Student), CancellationToken.None);

        Assert.Equal(new CreateWalletCommand(5), Assert.Single(_service.Creations));
        Assert.Empty(_service.Credits);
    }

    [Fact]
    public async Task UserRegistered_WhenTheServiceFails_DoesNotThrow()
    {
        _service.Fail = true;
        var handler = new CreateWalletEventHandler(_service, NullLogger<CreateWalletEventHandler>.Instance);

        await handler.HandleAsync(new UserRegistered(5, UserRole.Student), CancellationToken.None);

        Assert.Single(_service.Creations);
    }

    [Theory]
    [InlineData(ReviewDecision.Approved)]
    [InlineData(ReviewDecision.Rejected)]
    public async Task CaseResolved_CreditsTheVerifierWhateverTheDecision(ReviewDecision decision)
    {
        var handler = new CreditVerifierEventHandler(_service, NullLogger<CreditVerifierEventHandler>.Instance);

        await handler.HandleAsync(new VerificationCaseResolved(10, 1, 2, 5, "http-basics", decision),
            CancellationToken.None);

        Assert.Equal(new CreditVerifierCommand(2, 10), Assert.Single(_service.Credits));
        Assert.Empty(_service.Creations);
    }

    [Fact]
    public async Task CaseResolved_WhenTheServiceFails_DoesNotThrow()
    {
        _service.Fail = true;
        var handler = new CreditVerifierEventHandler(_service, NullLogger<CreditVerifierEventHandler>.Instance);

        await handler.HandleAsync(new VerificationCaseResolved(10, 1, 2, 5, "http-basics", ReviewDecision.Approved),
            CancellationToken.None);

        Assert.Single(_service.Credits);
    }

    private sealed class RecordingWalletCommandService : IWalletCommandService
    {
        public List<CreateWalletCommand> Creations { get; } = [];
        public List<CreditVerifierCommand> Credits { get; } = [];
        public bool Fail { get; set; }

        public Task<Result<Wallet>> Handle(CreateWalletCommand command, CancellationToken cancellationToken)
        {
            Creations.Add(command);
            return Task.FromResult(Result(new Wallet(command.OwnerId)));
        }

        public Task<Result<Wallet>> Handle(CreditVerifierCommand command, CancellationToken cancellationToken)
        {
            Credits.Add(command);
            return Task.FromResult(Result(new Wallet(command.VerifierUserId)));
        }

        public Task<Result<CreditTransaction>> Handle(RedeemCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("The handlers never redeem.");
        }

        private Result<Wallet> Result(Wallet wallet)
        {
            return Fail
                ? Result<Wallet>.Failure(RecognitionIncentivesError.DatabaseError, "failure")
                : Result<Wallet>.Success(wallet);
        }
    }
}