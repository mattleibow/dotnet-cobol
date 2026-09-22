using System.Collections.Immutable;

namespace Cobol.Compiler;

internal sealed class Binder
{
    private readonly CompilationUnitSyntax _root;
    private readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
    private readonly ImmutableDictionary<string, Symbol>.Builder _declarations = ImmutableDictionary.CreateBuilder<string, Symbol>(StringComparer.OrdinalIgnoreCase);
    private readonly ImmutableDictionary<SyntaxNode, Symbol>.Builder _bindings = ImmutableDictionary.CreateBuilder<SyntaxNode, Symbol>();

    public Binder(CompilationUnitSyntax root) => _root = root;

    public BindResult Bind()
    {
        ProgramSymbol? program = null;
        if (_root.ProgramId is { } programId)
        {
            program = new ProgramSymbol(programId.Identifier.Text, programId);
            Declare(programId, program);
        }

        var fields = ImmutableArray.CreateBuilder<FieldSymbol>();
        foreach (var item in _root.DataItems)
        {
            var field = new FieldSymbol(item.Identifier.Text, InferType(item.Picture), item);
            fields.Add(field);
            Declare(item, field);
        }

        var paragraphs = ImmutableArray.CreateBuilder<ParagraphSymbol>();
        foreach (var paragraph in Descendants<ParagraphSyntax>(_root))
        {
            var symbol = new ParagraphSymbol(paragraph.Identifier.Text, paragraph);
            paragraphs.Add(symbol);
            Declare(paragraph, symbol);
        }

        var scope = new Scope(_declarations.ToImmutable());
        foreach (var statement in _root.Statements) BindStatement(statement, scope);
        var model = new SemanticModel(program, fields.ToImmutable(), paragraphs.ToImmutable(), _declarations.ToImmutable(), _bindings.ToImmutable());
        return new BindResult(model, new BoundBlock([.. _root.Statements.Select(static statement => new BoundSourceStatement(statement))]), _diagnostics.ToImmutable());
    }

    private void BindStatement(StatementSyntax statement, Scope scope)
    {
        switch (statement)
        {
            case DisplayStatementSyntax display:
                foreach (var operand in display.Operands) BindExpression(operand, scope);
                break;
            case MoveStatementSyntax move:
                BindExpression(move.Source, scope); BindExpression(move.Destination, scope);
                break;
            case AcceptStatementSyntax accept:
                BindExpression(accept.Destination, scope);
                break;
            case ArithmeticStatementSyntax arithmetic:
                BindExpression(arithmetic.Operand, scope); BindExpression(arithmetic.Destination, scope);
                break;
            case IfStatementSyntax conditional:
                BindExpression(conditional.Left, scope); BindExpression(conditional.Right, scope);
                if (conditional.Statement is not null) BindStatement(conditional.Statement, scope);
                break;
            case UnsupportedStatementSyntax unsupported:
                var keyword = unsupported.StatementTokens.IsDefaultOrEmpty ? "<missing>" : unsupported.StatementTokens[0].Text;
                _diagnostics.Add(new Diagnostic(Diagnostics.UnsupportedConstruct, unsupported.SpanLocation(), keyword, CobolProfile.Name));
                break;
        }
    }

    private void BindExpression(ExpressionSyntax expression, Scope scope)
    {
        if (expression is not NameExpressionSyntax name) return;
        if (!scope.TryLookup(name.Identifier.Text, out var symbol) || symbol is not FieldSymbol)
        {
            _diagnostics.Add(new Diagnostic(Diagnostics.UnknownName, name.Identifier.Location, name.Identifier.Text));
            return;
        }

        _bindings[name] = symbol;
    }

    private void Declare(SyntaxNode declaration, Symbol symbol)
    {
        _declarations[symbol.Name] = symbol;
        _bindings[declaration] = symbol;
    }

    private static TypeSymbol InferType(string picture)
    {
        var normalized = picture.ToUpperInvariant();
        return normalized.Contains('X', StringComparison.Ordinal) ? TypeSymbol.Alphanumeric :
            normalized.Contains('V', StringComparison.Ordinal) ? TypeSymbol.FixedPoint : TypeSymbol.WholeNumber;
    }

    private static IEnumerable<TNode> Descendants<TNode>(SyntaxNode node) where TNode : SyntaxNode
    {
        foreach (var child in node.ChildNodes)
        {
            if (child is TNode typed) yield return typed;
            foreach (var descendant in Descendants<TNode>(child)) yield return descendant;
        }
    }
}
