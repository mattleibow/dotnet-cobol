using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A data-name reference expression.</summary>
public sealed class NameExpressionSyntax : ExpressionSyntax
{
    internal NameExpressionSyntax(SyntaxToken identifier) : base(SyntaxKind.NameExpression, [identifier]) => Identifier = identifier;
    /// <summary>Gets the referenced data-name token.</summary>
    public SyntaxToken Identifier { get; }
}
