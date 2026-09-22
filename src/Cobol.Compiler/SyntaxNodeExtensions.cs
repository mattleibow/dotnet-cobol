namespace Cobol.Compiler;

internal static class SyntaxNodeExtensions
{
    public static Location SpanLocation(this SyntaxNode node)
    {
        var token = node.Tokens.FirstOrDefault() ?? node.ChildNodes.SelectMany(static child => child.Tokens).FirstOrDefault();
        return token?.Location ?? new Location(new SourceText(string.Empty), new TextSpan(0, 0));
    }
}
