namespace Cobol.Compiler;

/// <summary>Represents the single declared COBOL program.</summary>
public sealed class ProgramSymbol : Symbol
{
    internal ProgramSymbol(string name, ProgramIdSyntax declaration) : base(name, SymbolKind.Program, declaration.Identifier.Location) => Declaration = declaration;
    /// <summary>Gets the PROGRAM-ID declaration.</summary>
    public ProgramIdSyntax Declaration { get; }
}
