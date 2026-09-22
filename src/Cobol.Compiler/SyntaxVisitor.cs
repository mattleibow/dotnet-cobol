namespace Cobol.Compiler;

/// <summary>Base visitor for immutable syntax trees.</summary>
public abstract class SyntaxVisitor
{
    /// <summary>Visits a node and its descendants.</summary>
    public virtual void Visit(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        foreach (var child in node.ChildNodes) child.Accept(this);
    }
}
