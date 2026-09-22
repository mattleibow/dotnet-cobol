using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Base class for immutable red syntax nodes. Child relationships and source spans are queryable.</summary>
public abstract class SyntaxNode
{
    private readonly ImmutableArray<SyntaxNode> _children;

    internal SyntaxNode(SyntaxKind kind, ImmutableArray<SyntaxNode> children, ImmutableArray<SyntaxToken> tokens)
    {
        Kind = kind;
        _children = children;
        Tokens = tokens;
        foreach (var child in children) child.Parent = this;
    }

    /// <summary>Gets the node kind.</summary>
    public SyntaxKind Kind { get; }
    /// <summary>Gets the parent node, or <see langword="null"/> for a tree root.</summary>
    public SyntaxNode? Parent { get; private set; }
    /// <summary>Gets direct children in source order.</summary>
    public ImmutableArray<SyntaxNode> ChildNodes => _children;
    /// <summary>Gets tokens directly owned by this node in source order.</summary>
    public ImmutableArray<SyntaxToken> Tokens { get; }
    /// <summary>Gets the first source position of this node.</summary>
    public int Position => FullSpan.Start;
    /// <summary>Gets the node span excluding leading trivia.</summary>
    public TextSpan Span => GetSpan(includeTrivia: false);
    /// <summary>Gets the node span including leading trivia.</summary>
    public TextSpan FullSpan => GetSpan(includeTrivia: true);

    /// <summary>Enumerates this node's descendant nodes and tokens in source order.</summary>
    public IEnumerable<object> DescendantNodesAndTokens()
    {
        foreach (var child in _children)
        {
            yield return child;
            foreach (var descendant in child.DescendantNodesAndTokens()) yield return descendant;
        }

        foreach (var token in Tokens) yield return token;
    }

    /// <summary>Reconstructs source text represented by the node.</summary>
    public virtual string ToFullString()
    {
        var items = DescendantNodesAndTokens().OfType<SyntaxToken>()
            .Concat(Tokens)
            .OrderBy(static token => token.FullSpan.Start)
            .ThenBy(static token => token.Span.Length);
        return string.Concat(items.Select(static token => token.ToFullString()));
    }

    /// <summary>Accepts a syntax visitor.</summary>
    public virtual void Accept(SyntaxVisitor visitor) => visitor.Visit(this);

    private TextSpan GetSpan(bool includeTrivia)
    {
        var spans = DescendantNodesAndTokens().OfType<SyntaxToken>().Concat(Tokens)
            .Select(token => includeTrivia ? token.FullSpan : token.Span).ToArray();
        return spans.Length == 0 ? new TextSpan(0, 0) : new TextSpan(spans.Min(static span => span.Start), spans.Max(static span => span.End) - spans.Min(static span => span.Start));
    }
}
