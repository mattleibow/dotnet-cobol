using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Creates immutable syntax tokens and small syntax fragments for tools and tests.</summary>
public static class SyntaxFactory
{
    /// <summary>Creates an identifier token outside a source tree.</summary>
    public static SyntaxToken Identifier(string text) => new(SyntaxKind.WordToken, text, null, new Location(new SourceText(text), new TextSpan(0, text.Length)), []);

    /// <summary>Creates a missing token at an insertion location.</summary>
    public static SyntaxToken MissingToken(SyntaxKind expectedKind, Location location) => new(SyntaxKind.MissingToken, string.Empty, null, location, [], true);

    /// <summary>Creates a name expression.</summary>
    public static NameExpressionSyntax Name(string text) => new(Identifier(text));

    /// <summary>Creates a literal expression.</summary>
    public static LiteralExpressionSyntax Literal(string text)
    {
        var kind = text.StartsWith('\'') || text.StartsWith('"') ? SyntaxKind.StringToken : SyntaxKind.NumberToken;
        return new LiteralExpressionSyntax(new SyntaxToken(kind, text, text, new Location(new SourceText(text), new TextSpan(0, text.Length)), []));
    }
}
