using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A parsed COBOL division, preserving its header and contained sections.</summary>
public sealed class DivisionSyntax : SyntaxNode
{
    internal DivisionSyntax(SyntaxToken name, SyntaxToken divisionKeyword, SyntaxToken period, ImmutableArray<SyntaxNode> members)
        : base(SyntaxKind.Division, members, [name, divisionKeyword, period])
    {
        Name = name; DivisionKeyword = divisionKeyword; PeriodToken = period; Members = members;
    }

    /// <summary>Gets the division-name token.</summary>
    public SyntaxToken Name { get; }
    /// <summary>Gets the DIVISION token.</summary>
    public SyntaxToken DivisionKeyword { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
    /// <summary>Gets contained declarations, sections, or paragraphs.</summary>
    public ImmutableArray<SyntaxNode> Members { get; }
}
