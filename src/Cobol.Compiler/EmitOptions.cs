namespace Cobol.Compiler;

/// <summary>Configures managed assembly emission.</summary>
public sealed record EmitOptions(string AssemblyName, bool OutputLibrary = false, bool Deterministic = true);
