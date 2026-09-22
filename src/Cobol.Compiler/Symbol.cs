namespace Cobol.Compiler;

/// <summary>Base class for immutable, queryable semantic symbols.</summary>
public abstract class Symbol
{
    internal Symbol(string name, SymbolKind kind, Location location) { Name = name; Kind = kind; Location = location; }
    /// <summary>Gets the case-preserved declared name.</summary>
    public string Name { get; }
    /// <summary>Gets the symbol category.</summary>
    public SymbolKind Kind { get; }
    /// <summary>Gets the declaration location.</summary>
    public Location Location { get; }
}
