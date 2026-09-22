using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Cobol.Compiler;

/// <summary>Base class for immutable symbols.</summary>
public abstract record Symbol(string Name);

/// <summary>Represents a COBOL elementary working-storage item.</summary>
public sealed record DataItemSymbol(string Name, CobolType Type, DataItemSyntax Declaration) : Symbol(Name);

/// <summary>The limited runtime type model for the first profile.</summary>
public enum CobolType { Alphanumeric, WholeNumber, FixedPoint }

/// <summary>Resolves syntax names and exposes immutable symbol information.</summary>
public sealed class SemanticModel
{
    private readonly ImmutableDictionary<string, DataItemSymbol> _symbols;

    internal SemanticModel(ImmutableDictionary<string, DataItemSymbol> symbols) => _symbols = symbols;

    /// <summary>Gets all declared data items.</summary>
    public ImmutableArray<DataItemSymbol> DataItems => [.. _symbols.Values];

    /// <summary>Resolves a data name using COBOL's case-insensitive comparison.</summary>
    public DataItemSymbol? GetDataItem(string name) => _symbols.GetValueOrDefault(name);
}

/// <summary>Base type for bound, validated statements.</summary>
public abstract record BoundStatement;

/// <summary>A sequence of bound statements.</summary>
public sealed record BoundBlock(ImmutableArray<BoundStatement> Statements) : BoundStatement;

/// <summary>A bound source statement kept for the isolated bootstrap backend.</summary>
public sealed record BoundSourceStatement(StatementSyntax Syntax) : BoundStatement;

internal sealed record BindResult(SemanticModel SemanticModel, BoundBlock Root, ImmutableArray<Diagnostic> Diagnostics);

internal static class Binder
{
    public static BindResult Bind(CompilationUnitSyntax root)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var symbols = ImmutableDictionary.CreateBuilder<string, DataItemSymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in root.DataItems)
        {
            var type = InferType(item.Picture);
            symbols[item.Name] = new DataItemSymbol(item.Name, type, item);
        }

        foreach (var statement in root.Statements) Validate(statement, symbols, diagnostics);
        var model = new SemanticModel(symbols.ToImmutable());
        var bound = new BoundBlock([.. root.Statements.Select(static statement => new BoundSourceStatement(statement))]);
        return new BindResult(model, bound, diagnostics.ToImmutable());
    }

    private static void Validate(StatementSyntax statement, ImmutableDictionary<string, DataItemSymbol>.Builder symbols, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        switch (statement)
        {
            case MoveStatementSyntax move:
                RequireName(move.Destination, move.Location, symbols, diagnostics);
                RequireNameIfIdentifier(move.Source, move.Location, symbols, diagnostics);
                break;
            case AcceptStatementSyntax accept:
                RequireName(accept.Destination, accept.Location, symbols, diagnostics);
                break;
            case ArithmeticStatementSyntax arithmetic:
                RequireName(arithmetic.Destination, arithmetic.Location, symbols, diagnostics);
                RequireNameIfIdentifier(arithmetic.Operand, arithmetic.Location, symbols, diagnostics);
                break;
            case DisplayStatementSyntax display:
                foreach (var operand in display.Operands) RequireNameIfIdentifier(operand, display.Location, symbols, diagnostics);
                break;
            case IfStatementSyntax conditional:
                RequireNameIfIdentifier(conditional.Left, conditional.Location, symbols, diagnostics);
                RequireNameIfIdentifier(conditional.Right, conditional.Location, symbols, diagnostics);
                if (conditional.ThenStatement is not null) Validate(conditional.ThenStatement, symbols, diagnostics);
                break;
            case UnsupportedStatementSyntax unsupported:
                diagnostics.Add(new Diagnostic(Diagnostics.UnsupportedConstruct, unsupported.Location, unsupported.Keyword, CobolProfile.Name));
                break;
        }
    }

    private static void RequireName(string name, Location location, ImmutableDictionary<string, DataItemSymbol>.Builder symbols, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (!symbols.ContainsKey(name)) diagnostics.Add(new Diagnostic(Diagnostics.UnknownName, location, name));
    }

    private static void RequireNameIfIdentifier(string text, Location location, ImmutableDictionary<string, DataItemSymbol>.Builder symbols, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (text.Length > 1 && text[0] is '\'' or '"' || decimal.TryParse(text, out _)) return;
        RequireName(text, location, symbols, diagnostics);
    }

    private static CobolType InferType(string picture)
    {
        var normalized = picture.ToUpperInvariant();
        if (normalized.Contains('X', StringComparison.Ordinal)) return CobolType.Alphanumeric;
        return normalized.Contains('V', StringComparison.Ordinal) ? CobolType.FixedPoint : CobolType.WholeNumber;
    }
}

/// <summary>Configures managed assembly emission.</summary>
public sealed record EmitOptions(string AssemblyName, bool OutputLibrary = false, bool Deterministic = true);

/// <summary>Returns immutable diagnostics and output status from a compilation.</summary>
public sealed record EmitResult(bool Success, ImmutableArray<Diagnostic> Diagnostics);

/// <summary>Represents a complete immutable COBOL compilation.</summary>
public sealed class Compilation
{
    private readonly ImmutableArray<SyntaxTree> _syntaxTrees;
    private readonly Lazy<BindResult> _binding;

    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees)
    {
        _syntaxTrees = syntaxTrees;
        _binding = new Lazy<BindResult>(() => Binder.Bind(_syntaxTrees[0].Root));
    }

    /// <summary>Gets syntax trees in the compilation.</summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees => _syntaxTrees;

    /// <summary>Creates a single-program compilation.</summary>
    public static Compilation Create(params SyntaxTree[] syntaxTrees)
    {
        ArgumentNullException.ThrowIfNull(syntaxTrees);
        if (syntaxTrees.Length != 1) throw new ArgumentException("The 0.1 profile accepts exactly one program source.", nameof(syntaxTrees));
        return new Compilation([.. syntaxTrees]);
    }

    /// <summary>Gets all parse and binding diagnostics without running the backend.</summary>
    public ImmutableArray<Diagnostic> GetDiagnostics() => [.. _syntaxTrees.SelectMany(static tree => tree.Diagnostics), .. _binding.Value.Diagnostics];

    /// <summary>Gets semantic data-item information.</summary>
    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
    {
        if (!_syntaxTrees.Contains(syntaxTree)) throw new ArgumentException("The syntax tree does not belong to this compilation.", nameof(syntaxTree));
        return _binding.Value.SemanticModel;
    }

    /// <summary>Emits a deterministic managed PE and optional portable PDB.</summary>
    public EmitResult Emit(Stream peStream, Stream? pdbStream, EmitOptions options)
    {
        ArgumentNullException.ThrowIfNull(peStream);
        ArgumentNullException.ThrowIfNull(options);
        var allDiagnostics = GetDiagnostics().ToBuilder();
        if (allDiagnostics.Any(static diagnostic => diagnostic.IsError)) return new EmitResult(false, allDiagnostics.ToImmutable());

        var lowered = Lowerer.Lower(_syntaxTrees[0].Root, _binding.Value.SemanticModel);
        var syntaxTree = CSharpSyntaxTree.ParseText(lowered);
        var csharp = CSharpCompilation.Create(
            options.AssemblyName,
            [syntaxTree],
            GetFrameworkReferences(),
            new CSharpCompilationOptions(
                options.OutputLibrary ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication,
                deterministic: options.Deterministic,
                optimizationLevel: OptimizationLevel.Release));

        var result = csharp.Emit(peStream, pdbStream, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
        foreach (var diagnostic in result.Diagnostics.Where(static diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
        {
            var location = new Location(_syntaxTrees[0].Text, new TextSpan(0, 0));
            allDiagnostics.Add(new Diagnostic(Diagnostics.EmitFailure, location, diagnostic.ToString()));
        }

        return new EmitResult(result.Success, allDiagnostics.ToImmutable());
    }

    private static IEnumerable<MetadataReference> GetFrameworkReferences()
    {
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("The runtime did not expose trusted platform assemblies.");
        return trusted.Split(Path.PathSeparator).Select(static path => MetadataReference.CreateFromFile(path));
    }
}

internal static class Lowerer
{
    public static string Lower(CompilationUnitSyntax root, SemanticModel semanticModel)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("internal static class CobolProgram");
        builder.AppendLine("{");
        builder.AppendLine("    private static void Main()");
        builder.AppendLine("    {");
        foreach (var item in root.DataItems) builder.Append("        ").Append(Declaration(item, semanticModel)).AppendLine(";");
        foreach (var statement in root.Statements) WriteStatement(builder, statement, semanticModel, 8);
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string Declaration(DataItemSyntax item, SemanticModel model)
    {
        var symbol = model.GetDataItem(item.Name)!;
        var initializer = item.Value is null ? Default(symbol.Type) : Expression(item.Value, model);
        return $"{TypeName(symbol.Type)} {Name(item.Name)} = {initializer}";
    }

    private static void WriteStatement(StringBuilder builder, StatementSyntax statement, SemanticModel model, int indentation)
    {
        var indent = new string(' ', indentation);
        switch (statement)
        {
            case DisplayStatementSyntax display:
                builder.Append(indent).Append("Console.WriteLine(").Append(string.Join(" + ", display.Operands.Select(operand => Expression(operand, model)))).AppendLine(");");
                break;
            case MoveStatementSyntax move:
                builder.Append(indent).Append(Name(move.Destination)).Append(" = ").Append(Expression(move.Source, model)).AppendLine(";");
                break;
            case AcceptStatementSyntax accept:
                builder.Append(indent).Append(Name(accept.Destination)).Append(" = ").Append(AcceptExpression(accept.Destination, model)).AppendLine(";");
                break;
            case ArithmeticStatementSyntax arithmetic:
                builder.Append(indent).Append(Name(arithmetic.Destination)).Append(" = ").Append(Name(arithmetic.Destination)).Append(' ').Append(ArithmeticOperator(arithmetic.Operation)).Append(' ').Append(Expression(arithmetic.Operand, model)).AppendLine(";");
                break;
            case IfStatementSyntax conditional:
                builder.Append(indent).Append("if (").Append(Expression(conditional.Left, model)).Append(' ').Append(RelationalOperator(conditional.Operator)).Append(' ').Append(Expression(conditional.Right, model)).AppendLine(")");
                builder.Append(indent).AppendLine("{");
                if (conditional.ThenStatement is not null) WriteStatement(builder, conditional.ThenStatement, model, indentation + 4);
                builder.Append(indent).AppendLine("}");
                break;
            case ExitStatementSyntax:
                builder.Append(indent).AppendLine("return;");
                break;
        }
    }

    private static string AcceptExpression(string destination, SemanticModel model) => model.GetDataItem(destination)?.Type switch
    {
        CobolType.Alphanumeric => "Console.ReadLine() ?? string.Empty",
        CobolType.WholeNumber => "int.TryParse(Console.ReadLine(), out var value) ? value : 0",
        _ => "decimal.TryParse(Console.ReadLine(), out var value) ? value : 0m",
    };
    private static string TypeName(CobolType type) => type switch { CobolType.Alphanumeric => "string", CobolType.WholeNumber => "int", _ => "decimal" };
    private static string Default(CobolType type) => type switch { CobolType.Alphanumeric => "string.Empty", CobolType.WholeNumber => "0", _ => "0m" };
    private static string Name(string cobolName) => "v_" + cobolName.Replace('-', '_');
    private static string Expression(string value, SemanticModel model)
    {
        if (value.Length > 1 && value[0] is '\'' or '"') return "\"" + value[1..^1].Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)) return decimalValue.ToString(CultureInfo.InvariantCulture) + (value.Contains('.') ? "m" : string.Empty);
        return Name(value);
    }

    private static string ArithmeticOperator(string operation) => operation.ToUpperInvariant() switch { "ADD" => "+", "SUBTRACT" => "-", "MULTIPLY" => "*", "DIVIDE" => "/", _ => throw new InvalidOperationException() };
    private static string RelationalOperator(string operation) => operation.ToUpperInvariant() switch { "=" or "EQUAL" => "==", ">" or "GREATER" => ">", "<" or "LESS" => "<", ">=" => ">=", "<=" => "<=", _ => "==" };
}
