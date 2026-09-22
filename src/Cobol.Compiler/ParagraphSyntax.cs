using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A procedure paragraph and its statements.</summary>
public sealed class ParagraphSyntax : SyntaxNode
{
    internal ParagraphSyntax(SyntaxToken identifier, SyntaxToken period, ImmutableArray<StatementSyntax> statements)
        : base(SyntaxKind.Paragraph, [.. statements], [identifier, period])
    {
        Identifier = identifier; PeriodToken = period; Statements = statements;
    }

    /// <summary>Gets the paragraph name.</summary>
    public SyntaxToken Identifier { get; }
    /// <summary>Gets the paragraph-header period.</summary>
    public SyntaxToken PeriodToken { get; }
    /// <summary>Gets contained statements.</summary>
    public ImmutableArray<StatementSyntax> Statements { get; }
}
