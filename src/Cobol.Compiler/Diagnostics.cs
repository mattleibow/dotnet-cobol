namespace Cobol.Compiler;

/// <summary>Catalog of diagnostics emitted by the initial profile.</summary>
public static class Diagnostics
{
    /// <summary>Unexpected source token.</summary>
    public static readonly DiagnosticDescriptor UnexpectedToken = new("COB1001", "Unexpected token", "Expected {0}, but found '{1}'.", DiagnosticSeverity.Error);
    /// <summary>Unsupported profile construct.</summary>
    public static readonly DiagnosticDescriptor UnsupportedConstruct = new("COB2001", "Unsupported construct", "'{0}' is not supported by {1}.", DiagnosticSeverity.Error);
    /// <summary>Missing program identifier.</summary>
    public static readonly DiagnosticDescriptor MissingProgramId = new("COB2002", "Missing PROGRAM-ID", "IDENTIFICATION DIVISION must contain PROGRAM-ID.", DiagnosticSeverity.Error);
    /// <summary>Unresolved identifier.</summary>
    public static readonly DiagnosticDescriptor UnknownName = new("COB3001", "Unknown data name", "Data item '{0}' was not declared in WORKING-STORAGE.", DiagnosticSeverity.Error);
    /// <summary>Emission failure.</summary>
    public static readonly DiagnosticDescriptor EmitFailure = new("COB9001", "Emission failed", "{0}", DiagnosticSeverity.Error);
}
