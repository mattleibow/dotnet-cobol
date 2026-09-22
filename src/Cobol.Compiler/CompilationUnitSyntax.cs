using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Root of a parsed single-program COBOL syntax tree.</summary>
public sealed class CompilationUnitSyntax : SyntaxNode
{
    internal CompilationUnitSyntax(ImmutableArray<DivisionSyntax> divisions, SyntaxToken endOfFileToken)
        : base(SyntaxKind.CompilationUnit, [.. divisions], [endOfFileToken])
    {
        Divisions = divisions; EndOfFileToken = endOfFileToken;
    }

    /// <summary>Gets divisions in source order.</summary>
    public ImmutableArray<DivisionSyntax> Divisions { get; }
    /// <summary>Gets the final token.</summary>
    public SyntaxToken EndOfFileToken { get; }
    /// <summary>Gets the PROGRAM-ID paragraph, if present.</summary>
    public ProgramIdSyntax? ProgramId => Divisions.SelectMany(static division => division.Members).OfType<ProgramIdSyntax>().FirstOrDefault();
    /// <summary>Gets elementary data declarations across data divisions.</summary>
    public ImmutableArray<DataItemSyntax> DataItems => [.. Divisions.SelectMany(static division => division.Members).SelectMany(Flatten).OfType<DataItemSyntax>()];
    /// <summary>Gets procedure statements across paragraphs and direct division members.</summary>
    public ImmutableArray<StatementSyntax> Statements => [.. Divisions.SelectMany(static division => division.Members).SelectMany(Flatten).OfType<StatementSyntax>()];

    private static IEnumerable<SyntaxNode> Flatten(SyntaxNode node)
    {
        yield return node;
        foreach (var child in node.ChildNodes)
        {
            foreach (var descendant in Flatten(child)) yield return descendant;
        }
    }
}
