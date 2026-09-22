using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Base type for immutable procedure-division statements.</summary>
public abstract class StatementSyntax : SyntaxNode
{
    internal StatementSyntax(SyntaxKind kind, ImmutableArray<SyntaxNode> children, ImmutableArray<SyntaxToken> tokens) : base(kind, children, tokens) { }
}
