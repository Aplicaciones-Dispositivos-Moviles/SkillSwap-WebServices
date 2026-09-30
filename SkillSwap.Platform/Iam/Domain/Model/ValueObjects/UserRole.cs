namespace SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     The role assigned to an account.
/// </summary>
/// <remarks>
///     The Verifier profile is not a role: it is an additional profile (VerifierProfile) that a
///     Student can acquire, managed in the Assessment &amp; Peer Review bounded context.
/// </remarks>
public enum UserRole
{
    Student,
    Coordinator
}