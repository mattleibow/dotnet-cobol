namespace Cobol.Compiler;

/// <summary>Immutable whitespace or comment trivia preserved alongside a token.</summary>
public sealed record SyntaxTrivia(string Text, TextSpan Span, bool IsComment);
