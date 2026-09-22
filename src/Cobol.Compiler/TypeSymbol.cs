namespace Cobol.Compiler;

/// <summary>Represents a built-in COBOL storage type in the supported profile.</summary>
public sealed class TypeSymbol : Symbol
{
    internal TypeSymbol(string name) : base(name, SymbolKind.Type, new Location(new SourceText(string.Empty), new TextSpan(0, 0))) { }
    /// <summary>Gets the alphanumeric string type.</summary>
    public static TypeSymbol Alphanumeric { get; } = new("alphanumeric");
    /// <summary>Gets the whole-number type.</summary>
    public static TypeSymbol WholeNumber { get; } = new("whole-number");
    /// <summary>Gets the fixed-point decimal type.</summary>
    public static TypeSymbol FixedPoint { get; } = new("fixed-point");
}
