namespace Cobol.Compiler;

/// <summary>A validated source statement retained by the initial lowering backend.</summary>
public sealed class BoundSourceStatement : BoundStatement
{
    internal BoundSourceStatement(StatementSyntax syntax) => Syntax = syntax;
    /// <summary>Gets the validated syntax statement.</summary>
    public StatementSyntax Syntax { get; }
}
