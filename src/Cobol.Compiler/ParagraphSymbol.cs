namespace Cobol.Compiler;

/// <summary>Represents a procedure paragraph.</summary>
public sealed class ParagraphSymbol : Symbol
{
    internal ParagraphSymbol(string name, ParagraphSyntax declaration) : base(name, SymbolKind.Paragraph, declaration.Identifier.Location) => Declaration = declaration;
    /// <summary>Gets the declaring paragraph syntax.</summary>
    public ParagraphSyntax Declaration { get; }
}
