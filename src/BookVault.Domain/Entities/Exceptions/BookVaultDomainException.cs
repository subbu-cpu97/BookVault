namespace BookVault.Domain.Exceptions;

// Custom exception — tells callers "this is a business rule violation"
// Inherits from Exception (OOP inheritance) — is-a Exception
public class BookVaultDomainException(string message) : Exception(message);
