using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A recovered statement outside the supported profile.</summary>
public sealed class UnsupportedStatementSyntax : StatementSyntax
{
    internal UnsupportedStatementSyntax(ImmutableArray<SyntaxToken> tokens)
        : base(SyntaxKind.UnsupportedStatement, [], tokens) => StatementTokens = tokens;

    /// <summary>Gets skipped tokens retained for diagnostics and round-tripping.</summary>
    public ImmutableArray<SyntaxToken> StatementTokens { get; }
}
