namespace Cobol.Compiler;

/// <summary>Represents an elementary WORKING-STORAGE declaration.</summary>
public sealed class FieldSymbol : Symbol
{
    internal FieldSymbol(string name, TypeSymbol type, DataItemSyntax declaration) : base(name, SymbolKind.Field, declaration.Identifier.Location)
    {
        Type = type; Declaration = declaration;
    }
    /// <summary>Gets the storage type.</summary>
    public TypeSymbol Type { get; }
    /// <summary>Gets the declaring syntax.</summary>
    public DataItemSyntax Declaration { get; }
}
