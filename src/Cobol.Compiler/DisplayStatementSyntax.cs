using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A DISPLAY statement.</summary>
public sealed class DisplayStatementSyntax : StatementSyntax
{
    internal DisplayStatementSyntax(SyntaxToken keyword, ImmutableArray<ExpressionSyntax> operands, SyntaxToken period)
        : base(SyntaxKind.DisplayStatement, [.. operands], [keyword, period])
    {
        Keyword = keyword;
        Operands = operands;
        PeriodToken = period;
    }

    /// <summary>Gets the DISPLAY keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets display operands.</summary>
    public ImmutableArray<ExpressionSyntax> Operands { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
}
