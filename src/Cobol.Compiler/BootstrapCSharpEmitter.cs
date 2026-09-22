using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Cobol.Compiler;

/// <summary>Temporary isolated managed backend that lowers bound COBOL to an in-memory C# compilation.</summary>
internal sealed class BootstrapCSharpEmitter
{
    public static EmitResult Emit(BoundBlock root, SemanticModel model, Stream peStream, Stream? pdbStream, EmitOptions options)
    {
        var source = BuildSource(root, model);
        var csharp = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            options.AssemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            GetFrameworkReferences(),
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(options.OutputLibrary ? Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary : Microsoft.CodeAnalysis.OutputKind.ConsoleApplication, deterministic: options.Deterministic, optimizationLevel: Microsoft.CodeAnalysis.OptimizationLevel.Release));
        var emit = csharp.Emit(peStream, pdbStream, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var diagnostic in emit.Diagnostics.Where(static diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
            diagnostics.Add(new Diagnostic(Diagnostics.EmitFailure, new Location(new SourceText(string.Empty), new TextSpan(0, 0)), diagnostic.ToString()));
        return new EmitResult(emit.Success, diagnostics.ToImmutable());
    }

    private static string BuildSource(BoundBlock root, SemanticModel model)
    {
        var builder = new StringBuilder("using System;\nusing System.Globalization;\ninternal static class CobolProgram\n{\n    private static void Main()\n    {\n");
        foreach (var field in model.Fields) builder.Append("        ").Append(TypeName(field.Type)).Append(' ').Append(Name(field.Name)).Append(" = ").Append(field.Declaration.Value is null ? Default(field.Type) : Expression(field.Declaration.Value, model)).AppendLine(";");
        foreach (var statement in root.Statements.OfType<BoundSourceStatement>()) WriteStatement(builder, statement.Syntax, model, 8);
        return builder.Append("    }\n}\n").ToString();
    }

    private static void WriteStatement(StringBuilder builder, StatementSyntax statement, SemanticModel model, int indent)
    {
        var spaces = new string(' ', indent);
        switch (statement)
        {
            case DisplayStatementSyntax display:
                builder.Append(spaces).Append("Console.WriteLine(").Append(string.Join(" + ", display.Operands.Select(operand => Expression(operand, model)))).AppendLine(");"); break;
            case MoveStatementSyntax move:
                builder.Append(spaces).Append(Name(move.Destination.Identifier.Text)).Append(" = ").Append(Expression(move.Source, model)).AppendLine(";"); break;
            case AcceptStatementSyntax accept:
                builder.Append(spaces).Append(Name(accept.Destination.Identifier.Text)).Append(" = ").Append(AcceptExpression(accept.Destination, model)).AppendLine(";"); break;
            case ArithmeticStatementSyntax arithmetic:
                builder.Append(spaces).Append(Name(arithmetic.Destination.Identifier.Text)).Append(" = ").Append(Name(arithmetic.Destination.Identifier.Text)).Append(' ').Append(Operator(arithmetic.Keyword.Text)).Append(' ').Append(Expression(arithmetic.Operand, model)).AppendLine(";"); break;
            case IfStatementSyntax conditional:
                builder.Append(spaces).Append("if (").Append(Expression(conditional.Left, model)).Append(' ').Append(Operator(conditional.OperatorToken.Text)).Append(' ').Append(Expression(conditional.Right, model)).AppendLine(")\n" + spaces + "{");
                if (conditional.Statement is not null) WriteStatement(builder, conditional.Statement, model, indent + 4);
                builder.Append(spaces).AppendLine("}"); break;
            case ExitStatementSyntax:
                builder.Append(spaces).AppendLine("return;"); break;
        }
    }

    private static string Expression(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        LiteralExpressionSyntax literal when literal.LiteralToken.Kind == SyntaxKind.StringToken => "\"" + literal.LiteralToken.Text[1..^1].Replace("\"", "\\\"", StringComparison.Ordinal) + "\"",
        LiteralExpressionSyntax literal when literal.LiteralToken.Text.Contains('.') => literal.LiteralToken.Text + "m",
        LiteralExpressionSyntax literal => literal.LiteralToken.Text,
        NameExpressionSyntax name => Name(name.Identifier.Text),
        _ => "default",
    };
    private static string AcceptExpression(NameExpressionSyntax destination, SemanticModel model) => model.GetTypeInfo(destination).Type switch
    {
        var type when type == TypeSymbol.Alphanumeric => "Console.ReadLine() ?? string.Empty",
        var type when type == TypeSymbol.WholeNumber => "int.TryParse(Console.ReadLine(), out var value) ? value : 0",
        _ => "decimal.TryParse(Console.ReadLine(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m",
    };
    private static string TypeName(TypeSymbol type) => type == TypeSymbol.Alphanumeric ? "string" : type == TypeSymbol.WholeNumber ? "int" : "decimal";
    private static string Default(TypeSymbol type) => type == TypeSymbol.Alphanumeric ? "string.Empty" : type == TypeSymbol.WholeNumber ? "0" : "0m";
    private static string Name(string name) => "v_" + name.Replace('-', '_');
    private static string Operator(string text) => text.ToUpperInvariant() switch { "ADD" => "+", "SUBTRACT" => "-", "MULTIPLY" => "*", "DIVIDE" => "/", "=" or "EQUAL" => "==", ">" => ">", "<" => "<", ">=" => ">=", "<=" => "<=", _ => "==" };
    private static IEnumerable<Microsoft.CodeAnalysis.MetadataReference> GetFrameworkReferences()
    {
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("The runtime did not expose trusted platform assemblies.");
        return trusted.Split(Path.PathSeparator).Select(static path => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(path));
    }
}
