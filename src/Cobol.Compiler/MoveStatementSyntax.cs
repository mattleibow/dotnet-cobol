using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A MOVE statement.</summary>
public sealed class MoveStatementSyntax : StatementSyntax
{
    internal MoveStatementSyntax(SyntaxToken keyword, ExpressionSyntax source, SyntaxToken toKeyword, NameExpressionSyntax destination, SyntaxToken period)
        : base(SyntaxKind.MoveStatement, [source, destination], [keyword, toKeyword, period])
    {
        Keyword = keyword; Source = source; ToKeyword = toKeyword; Destination = destination; PeriodToken = period;
    }

    /// <summary>Gets the MOVE keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets the moved expression.</summary>
    public ExpressionSyntax Source { get; }
    /// <summary>Gets the TO keyword.</summary>
    public SyntaxToken ToKeyword { get; }
    /// <summary>Gets the destination.</summary>
    public NameExpressionSyntax Destination { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
}
