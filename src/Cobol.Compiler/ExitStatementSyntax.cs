using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A STOP RUN or GOBACK statement.</summary>
public sealed class ExitStatementSyntax : StatementSyntax
{
    internal ExitStatementSyntax(SyntaxToken keyword, SyntaxToken? runKeyword, SyntaxToken period)
        : base(SyntaxKind.ExitStatement, [], runKeyword is null ? [keyword, period] : [keyword, runKeyword, period])
    {
        Keyword = keyword; RunKeyword = runKeyword; PeriodToken = period;
    }

    /// <summary>Gets the STOP or GOBACK keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets the optional RUN keyword.</summary>
    public SyntaxToken? RunKeyword { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
}
