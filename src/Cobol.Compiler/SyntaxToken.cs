using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>An immutable lexical token, including its leading trivia and missing-token state.</summary>
public sealed class SyntaxToken
{
    internal SyntaxToken(SyntaxKind kind, string text, object? value, Location location, ImmutableArray<SyntaxTrivia> leadingTrivia, bool isMissing = false)
    {
        Kind = kind;
        Text = text;
        Value = value;
        Location = location;
        LeadingTrivia = leadingTrivia;
        IsMissing = isMissing;
    }

    /// <summary>Gets the token classification.</summary>
    public SyntaxKind Kind { get; }
    /// <summary>Gets source text, excluding trivia.</summary>
    public string Text { get; }
    /// <summary>Gets the decoded literal value when applicable.</summary>
    public object? Value { get; }
    /// <summary>Gets the source location, or insertion point for a missing token.</summary>
    public Location Location { get; }
    /// <summary>Gets the trivia directly preceding this token.</summary>
    public ImmutableArray<SyntaxTrivia> LeadingTrivia { get; }
    /// <summary>Gets whether this token was synthesized during error recovery.</summary>
    public bool IsMissing { get; }
    /// <summary>Gets the source span excluding trivia.</summary>
    public TextSpan Span => Location.Span;
    /// <summary>Gets the source span including leading trivia.</summary>
    public TextSpan FullSpan => LeadingTrivia.IsDefaultOrEmpty ? Span : new(LeadingTrivia[0].Span.Start, Span.End - LeadingTrivia[0].Span.Start);
    /// <summary>Gets preserved leading trivia plus token text.</summary>
    public string ToFullString() => string.Concat(LeadingTrivia.Select(static trivia => trivia.Text)) + Text;
}
