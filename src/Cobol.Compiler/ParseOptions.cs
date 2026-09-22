namespace Cobol.Compiler;

/// <summary>Controls source-format interpretation during parsing.</summary>
public sealed record ParseOptions(bool PreferFixedFormat = false)
{
    /// <summary>Gets the default source interpretation options.</summary>
    public static ParseOptions Default { get; } = new();
}
