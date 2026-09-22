using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>The PROGRAM-ID paragraph of an identification division.</summary>
public sealed class ProgramIdSyntax : SyntaxNode
{
    internal ProgramIdSyntax(SyntaxToken keyword, SyntaxToken firstPeriod, SyntaxToken identifier, SyntaxToken period)
        : base(SyntaxKind.ProgramId, [], [keyword, firstPeriod, identifier, period])
    {
        Keyword = keyword; FirstPeriod = firstPeriod; Identifier = identifier; PeriodToken = period;
    }

    /// <summary>Gets the PROGRAM-ID keyword.</summary>
    public SyntaxToken Keyword { get; }
    /// <summary>Gets the period after PROGRAM-ID.</summary>
    public SyntaxToken FirstPeriod { get; }
    /// <summary>Gets the declared program name.</summary>
    public SyntaxToken Identifier { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
}
