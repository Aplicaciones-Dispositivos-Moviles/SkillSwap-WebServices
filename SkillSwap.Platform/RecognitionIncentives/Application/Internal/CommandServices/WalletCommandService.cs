using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.RecognitionIncentives.Application.CommandServices;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;
using SkillSwap.Platform.RecognitionIncentives.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.RecognitionIncentives.Application.Internal.CommandServices;

/// <summary>
///     Wallet command service
/// </summary>
/// <param name="walletRepository">Wallet repository</param>
/// <param name="transactionRepository">Credit transaction repository</param>
/// <param name="redemptionPricing">Cost of each benefit</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class WalletCommandService(
    IWalletRepository walletRepository,
    ICreditTransactionRepository transactionRepository,
    IRedemptionPricing redemptionPricing,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<WalletCommandService> logger)
    : IWalletCommandService
{
    private const string EarnedDescription = "Verification case resolved";

    /// <inheritdoc />
    public async Task<Result<Wallet>> Handle(CreateWalletCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await walletRepository.FindByOwnerIdAsync(command.OwnerId, cancellationToken);
            if (existing is not null) return Result<Wallet>.Success(existing);

            var wallet = new Wallet(command.OwnerId);
            await walletRepository.AddAsync(wallet, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Wallet>.Success(wallet);
        }
        catch (Exception exception)
        {
            return FailureFrom<Wallet>(exception, "create the wallet of user {OwnerId}", command.OwnerId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Wallet>> Handle(CreditVerifierCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var wallet = await walletRepository.FindByOwnerIdAsync(command.VerifierUserId, cancellationToken);
            if (wallet is not null
                && await transactionRepository.ExistsEarnedForCaseAsync(wallet.Id, command.CaseId,
                    cancellationToken))
                return Result<Wallet>.Success(wallet);

            var isNew = wallet is null;
            wallet ??= new Wallet(command.VerifierUserId);
            var amount = CreditRewards.ForResolvedCase();
            wallet.Credit(amount);

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (isNew)
                {
                    // The movement needs the id of the wallet, so the wallet is saved first.
                    await walletRepository.AddAsync(wallet, cancellationToken);
                    await unitOfWork.CompleteAsync(cancellationToken);
                }
                else
                {
                    walletRepository.Update(wallet);
                }

                await transactionRepository.AddAsync(
                    new CreditTransaction(wallet.Id, amount, TransactionType.Earned, EarnedDescription,
                        command.CaseId), cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
            }, cancellationToken);

            return Result<Wallet>.Success(wallet);
        }
        catch (Exception exception)
        {
            return FailureFrom<Wallet>(exception, "credit verifier {VerifierUserId}", command.VerifierUserId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CreditTransaction>> Handle(RedeemCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.Item)) return Failure<CreditTransaction>(RecognitionIncentivesError.InvalidRedemptionItem);

        try
        {
            var wallet = await walletRepository.FindByOwnerIdAsync(command.UserId, cancellationToken);
            if (wallet is null) return Failure<CreditTransaction>(RecognitionIncentivesError.WalletNotFound);

            var cost = redemptionPricing.CalculateCost(command.Item);
            if (!wallet.CanAfford(cost)) return Failure<CreditTransaction>(RecognitionIncentivesError.InsufficientBalance);

            wallet.Debit(cost);
            var transaction = new CreditTransaction(wallet.Id, cost, TransactionType.Redeemed,
                $"Redeemed: {Describe(command.Item)}");

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                walletRepository.Update(wallet);
                await transactionRepository.AddAsync(transaction, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
            }, cancellationToken);

            return Result<CreditTransaction>.Success(transaction);
        }
        catch (Exception exception)
        {
            return FailureFrom<CreditTransaction>(exception, "redeem a benefit for user {UserId}", command.UserId);
        }
    }

    private static string Describe(RedemptionItem item)
    {
        return item switch
        {
            RedemptionItem.AdvancedPathUnlock => "advanced path unlock",
            RedemptionItem.ContributionCertificate => "contribution certificate",
            _ => item.ToString()
        };
    }

    private Result<T> Failure<T>(RecognitionIncentivesError error)
    {
        return Result<T>.Failure(error, localizer[error.ToString()]);
    }

    private Result<T> FailureFrom<T>(Exception exception, string operation, int id)
    {
        var error = RecognitionIncentivesErrors.FromException(exception);
        if (error != RecognitionIncentivesError.OperationCancelled)
            logger.LogError(exception, "Could not " + operation, id);
        return Failure<T>(error);
    }
}