namespace Cobol.Compiler;

/// <summary>Identifies a source range in a document.</summary>
public readonly record struct Location(SourceText Source, TextSpan Span)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var (line, column) = Source.GetLinePosition(Span.Start);
        return $"{(string.IsNullOrEmpty(Source.FilePath) ? "<source>" : Source.FilePath)}({line},{column})";
    }
}
