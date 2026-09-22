namespace Cobol.Compiler;

/// <summary>Defines a stable compiler diagnostic.</summary>
public sealed record DiagnosticDescriptor(string Id, string Title, string MessageFormat, DiagnosticSeverity Severity);
