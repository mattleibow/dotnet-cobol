namespace Cobol.Compiler;

/// <summary>References a managed metadata assembly used by a future direct IL emitter.</summary>
public sealed class MetadataReference
{
    private MetadataReference(string path) => FilePath = path;
    /// <summary>Gets the metadata assembly file path.</summary>
    public string FilePath { get; }
    /// <summary>Creates a metadata reference from an existing assembly path.</summary>
    public static MetadataReference CreateFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new MetadataReference(path);
    }
}
