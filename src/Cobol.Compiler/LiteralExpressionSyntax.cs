using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A numeric or quoted literal expression.</summary>
public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    internal LiteralExpressionSyntax(SyntaxToken literalToken) : base(SyntaxKind.LiteralExpression, [literalToken]) => LiteralToken = literalToken;
    /// <summary>Gets the literal token.</summary>
    public SyntaxToken LiteralToken { get; }
}
