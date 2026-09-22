using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A parsed COBOL section header and members.</summary>
public sealed class SectionSyntax : SyntaxNode
{
    internal SectionSyntax(SyntaxToken name, SyntaxToken sectionKeyword, SyntaxToken period, ImmutableArray<SyntaxNode> members)
        : base(SyntaxKind.Section, members, [name, sectionKeyword, period])
    {
        Name = name; SectionKeyword = sectionKeyword; PeriodToken = period; Members = members;
    }

    /// <summary>Gets the section name.</summary>
    public SyntaxToken Name { get; }
    /// <summary>Gets the SECTION keyword.</summary>
    public SyntaxToken SectionKeyword { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
    /// <summary>Gets declarations or statements in the section.</summary>
    public ImmutableArray<SyntaxNode> Members { get; }
}
