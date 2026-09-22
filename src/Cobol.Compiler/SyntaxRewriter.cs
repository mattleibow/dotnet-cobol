namespace Cobol.Compiler;

/// <summary>Base rewriter. Returning the existing node preserves structural sharing.</summary>
public abstract class SyntaxRewriter : SyntaxVisitor
{
    /// <summary>Visits a node. Derived classes may return a replacement tree node.</summary>
    public virtual SyntaxNode VisitAndRewrite(SyntaxNode node)
    {
        Visit(node);
        return node;
    }
}
