using System.Collections.Immutable;

namespace Cobol.Compiler;

internal sealed class BindResult
{
    public BindResult(SemanticModel semanticModel, BoundBlock root, ImmutableArray<Diagnostic> diagnostics)
    {
        SemanticModel = semanticModel; Root = root; Diagnostics = diagnostics;
    }

    public SemanticModel SemanticModel { get; }
    public BoundBlock Root { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
}
