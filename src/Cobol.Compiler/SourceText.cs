namespace Cobol.Compiler;

/// <summary>Immutable source text and its logical file path.</summary>
public sealed class SourceText
{
    /// <summary>Initializes a source document.</summary>
    public SourceText(string text, string filePath = "")
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        FilePath = filePath;
    }

    /// <summary>Gets the original, unnormalized document text.</summary>
    public string Text { get; }

    /// <summary>Gets the logical path displayed in diagnostics.</summary>
    public string FilePath { get; }

    /// <summary>Creates a source document from a UTF-8-compatible file.</summary>
    public static SourceText FromFile(string path) => new(File.ReadAllText(path), path);

    /// <summary>Gets the one-based source line and column at an offset.</summary>
    public (int Line, int Column) GetLinePosition(int offset)
    {
        var line = 1;
        var column = 1;
        foreach (var character in Text.AsSpan(0, Math.Clamp(offset, 0, Text.Length)))
        {
            if (character == '\n') { line++; column = 1; }
            else { column++; }
        }

        return (line, column);
    }
}
