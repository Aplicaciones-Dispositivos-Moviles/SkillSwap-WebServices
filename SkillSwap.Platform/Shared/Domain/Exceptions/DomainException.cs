namespace SkillSwap.Platform.Shared.Domain.Exceptions;

/// <summary>
///     Thrown when a domain invariant is violated (e.g. an invalid Value Object).
/// </summary>
/// <param name="message">A description of the violated rule.</param>
public class DomainException(string message) : Exception(message);