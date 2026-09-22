using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>An ADD, SUBTRACT, MULTIPLY, or DIVIDE statement.</summary>
public sealed class ArithmeticStatementSyntax : StatementSyntax
{
    internal ArithmeticStatementSyntax(SyntaxToken keyword, ExpressionSyntax operand, SyntaxToken directionKeyword, NameExpressionSyntax destination, SyntaxToken period)
        : base(SyntaxKind.ArithmeticStatement, [operand, destination], [keyword, directionKeyword, period])
    {
        Keyword = keyword; Operand = operand; DirectionKeyword = directionKeyword; Destination = destination; PeriodToken = period;
    }

    /// <summary>Gets the arithmetic keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets the arithmetic operand.</summary>
    public ExpressionSyntax Operand { get; }
    /// <summary>Gets the TO, FROM, or BY keyword.</summary>
    public SyntaxToken DirectionKeyword { get; }
    /// <summary>Gets the modified item.</summary>
    public NameExpressionSyntax Destination { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
}
