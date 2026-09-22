using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>An IF statement containing a supported single statement body.</summary>
public sealed class IfStatementSyntax : StatementSyntax
{
    internal IfStatementSyntax(SyntaxToken keyword, ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right, SyntaxToken? thenKeyword, StatementSyntax? statement)
        : base(SyntaxKind.IfStatement, statement is null ? [left, right] : [left, right, statement], thenKeyword is null ? [keyword, operatorToken] : [keyword, operatorToken, thenKeyword])
    {
        Keyword = keyword; Left = left; OperatorToken = operatorToken; Right = right; ThenKeyword = thenKeyword; Statement = statement;
    }

    /// <summary>Gets the IF keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets the left comparison expression.</summary>
    public ExpressionSyntax Left { get; }
    /// <summary>Gets the comparison operator.</summary>
    public SyntaxToken OperatorToken { get; }
    /// <summary>Gets the right comparison expression.</summary>
    public ExpressionSyntax Right { get; }
    /// <summary>Gets the optional THEN keyword.</summary>
    public SyntaxToken? ThenKeyword { get; }
    /// <summary>Gets the single supported conditional statement.</summary>
    public StatementSyntax? Statement { get; }
}
