using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>An immutable parsed COBOL document with source, root, tokens, and diagnostics.</summary>
public sealed class SyntaxTree
{
    internal SyntaxTree(SourceText text, CompilationUnitSyntax root, ImmutableArray<SyntaxToken> tokens, ImmutableArray<Diagnostic> diagnostics)
    {
        Text = text; Root = root; Tokens = tokens; Diagnostics = diagnostics;
    }

    /// <summary>Gets the original source document.</summary>
    public SourceText Text { get; }
    /// <summary>Gets the immutable red root node.</summary>
    public CompilationUnitSyntax Root { get; }
    /// <summary>Gets all lexical tokens including EOF and skipped/error-recovery tokens.</summary>
    public ImmutableArray<SyntaxToken> Tokens { get; }
    /// <summary>Gets lexer and parser diagnostics.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>Parses a COBOL source document using default options.</summary>
    public static SyntaxTree Parse(SourceText source) => Parse(source, ParseOptions.Default);

    /// <summary>Parses a COBOL source document with explicit source-format options.</summary>
    public static SyntaxTree Parse(SourceText source, ParseOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        var lexer = new Lexer(source, options);
        var tokens = lexer.Lex();
        return new Parser(source, tokens, lexer.Diagnostics).Parse();
    }

    /// <summary>Reconstructs the preserved source representation from root tokens.</summary>
    public string ToFullString() => Root.ToFullString();
}
