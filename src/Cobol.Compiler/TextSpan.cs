namespace Cobol.Compiler;

/// <summary>A contiguous zero-based source range.</summary>
public readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>Gets the offset immediately after the range.</summary>
    public int End => Start + Length;
}
