namespace Cobol.Compiler;

/// <summary>References a source document supplied to a compilation.</summary>
public sealed class SourceReference
{
    /// <summary>Initializes a source reference.</summary>
    public SourceReference(SourceText source) => Source = source ?? throw new ArgumentNullException(nameof(source));
    /// <summary>Gets the referenced document.</summary>
    public SourceText Source { get; }
}
