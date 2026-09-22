using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Immutable symbol and type binding results for a <see cref="SyntaxTree"/>.</summary>
public sealed class SemanticModel
{
    private readonly ImmutableDictionary<string, Symbol> _declarations;
    private readonly ImmutableDictionary<SyntaxNode, Symbol> _bindings;

    internal SemanticModel(ProgramSymbol? program, ImmutableArray<FieldSymbol> fields, ImmutableArray<ParagraphSymbol> paragraphs, ImmutableDictionary<string, Symbol> declarations, ImmutableDictionary<SyntaxNode, Symbol> bindings)
    {
        Program = program; Fields = fields; Paragraphs = paragraphs; _declarations = declarations; _bindings = bindings;
    }

    /// <summary>Gets the declared program.</summary>
    public ProgramSymbol? Program { get; }
    /// <summary>Gets declared working-storage fields.</summary>
    public ImmutableArray<FieldSymbol> Fields { get; }
    /// <summary>Gets declared procedure paragraphs.</summary>
    public ImmutableArray<ParagraphSymbol> Paragraphs { get; }
    /// <summary>Gets all declared symbols.</summary>
    public ImmutableArray<Symbol> DeclaredSymbols => [.. _declarations.Values];
    /// <summary>Resolves a declared name with COBOL case-insensitive lookup.</summary>
    public Symbol? GetDeclaredSymbol(string name) => _declarations.GetValueOrDefault(name);
    /// <summary>Gets semantic information for a declaration or bound reference.</summary>
    public SymbolInfo GetSymbolInfo(SyntaxNode node) => new(_bindings.GetValueOrDefault(node));
    /// <summary>Gets an expression's inferred storage type.</summary>
    public TypeInfo GetTypeInfo(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal when literal.LiteralToken.Kind == SyntaxKind.StringToken => new(TypeSymbol.Alphanumeric),
        LiteralExpressionSyntax literal when literal.LiteralToken.Text.Contains('.') => new(TypeSymbol.FixedPoint),
        LiteralExpressionSyntax => new(TypeSymbol.WholeNumber),
        NameExpressionSyntax name when _bindings.GetValueOrDefault(name) is FieldSymbol field => new(field.Type),
        _ => new(null),
    };
}
