using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>A lowered sequence of bound procedure statements.</summary>
public sealed class BoundBlock : BoundStatement
{
    internal BoundBlock(ImmutableArray<BoundStatement> statements) => Statements = statements;
    /// <summary>Gets statements in execution order.</summary>
    public ImmutableArray<BoundStatement> Statements { get; }
}
