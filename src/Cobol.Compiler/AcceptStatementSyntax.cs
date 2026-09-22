using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>An ACCEPT statement.</summary>
public sealed class AcceptStatementSyntax : StatementSyntax
{
    internal AcceptStatementSyntax(SyntaxToken keyword, NameExpressionSyntax destination, SyntaxToken period)
        : base(SyntaxKind.AcceptStatement, [destination], [keyword, period])
    {
        Keyword = keyword; Destination = destination; PeriodToken = period;
    }

    /// <summary>Gets the ACCEPT keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets the input destination.</summary>
    public NameExpressionSyntax Destination { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
}
