using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Base type for immutable COBOL expression syntax.</summary>
public abstract class ExpressionSyntax : SyntaxNode
{
    internal ExpressionSyntax(SyntaxKind kind, ImmutableArray<SyntaxToken> tokens) : base(kind, [], tokens) { }
}
