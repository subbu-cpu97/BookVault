namespace BookVault.Domain;

// Marker class — used by NetArchTest to locate this assembly
// Interview answer: "Why a marker class instead of typeof(SomeEntity)?"
// Entities come and go. The marker is permanent and intentional.
// It's a stable, explicit contract: "this type represents this assembly."
public sealed class AssemblyReference;
