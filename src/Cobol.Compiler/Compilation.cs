using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Represents an immutable, single-program COBOL compilation.</summary>
public sealed class Compilation
{
    private readonly Lazy<BindResult> _binding;

    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees, ImmutableArray<MetadataReference> metadataReferences)
    {
        SyntaxTrees = syntaxTrees;
        MetadataReferences = metadataReferences;
        _binding = new Lazy<BindResult>(() => new Binder(SyntaxTrees[0].Root).Bind(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets source syntax trees.</summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    /// <summary>Gets metadata references reserved for the direct emitter.</summary>
    public ImmutableArray<MetadataReference> MetadataReferences { get; }
    /// <summary>Gets source references derived from syntax trees.</summary>
    public ImmutableArray<SourceReference> SourceReferences => [.. SyntaxTrees.Select(static tree => new SourceReference(tree.Text))];

    /// <summary>Creates a compilation for exactly one program source.</summary>
    public static Compilation Create(params SyntaxTree[] syntaxTrees) => Create([], syntaxTrees);

    /// <summary>Creates a compilation with explicit managed metadata references.</summary>
    public static Compilation Create(ImmutableArray<MetadataReference> metadataReferences, params SyntaxTree[] syntaxTrees)
    {
        ArgumentNullException.ThrowIfNull(syntaxTrees);
        if (syntaxTrees.Length != 1) throw new ArgumentException("The 0.1 profile accepts exactly one program source.", nameof(syntaxTrees));
        return new Compilation([.. syntaxTrees], metadataReferences);
    }

    /// <summary>Creates a compilation with one syntax tree replaced.</summary>
    public Compilation ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
    {
        var index = SyntaxTrees.IndexOf(oldTree);
        if (index < 0) throw new ArgumentException("The syntax tree does not belong to this compilation.", nameof(oldTree));
        return new Compilation(SyntaxTrees.SetItem(index, newTree), MetadataReferences);
    }

    /// <summary>Gets parse and binding diagnostics.</summary>
    public ImmutableArray<Diagnostic> GetDiagnostics() => [.. SyntaxTrees.SelectMany(static tree => tree.Diagnostics), .. _binding.Value.Diagnostics];

    /// <summary>Gets semantic data for a tree owned by this compilation.</summary>
    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
    {
        if (!SyntaxTrees.Contains(syntaxTree)) throw new ArgumentException("The syntax tree does not belong to this compilation.", nameof(syntaxTree));
        return _binding.Value.SemanticModel;
    }

    /// <summary>Emits a managed PE and optional portable PDB.</summary>
    public EmitResult Emit(Stream peStream, Stream? pdbStream, EmitOptions options)
    {
        ArgumentNullException.ThrowIfNull(peStream);
        ArgumentNullException.ThrowIfNull(options);
        var diagnostics = GetDiagnostics().ToBuilder();
        if (diagnostics.Any(static diagnostic => diagnostic.IsError)) return new EmitResult(false, diagnostics.ToImmutable());
        var result = BootstrapCSharpEmitter.Emit(_binding.Value.Root, _binding.Value.SemanticModel, peStream, pdbStream, options);
        diagnostics.AddRange(result.Diagnostics);
        return new EmitResult(result.Success, diagnostics.ToImmutable());
    }
}
