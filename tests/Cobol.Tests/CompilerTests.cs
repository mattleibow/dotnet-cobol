using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Cobol.Compiler;

namespace Cobol.Tests;

internal static class CompilerTests
{
    public static int Run()
    {
        var failures = new List<string>();
        Run(failures, "builds complete tree and preserves source", BuildsCompleteTree);
        Run(failures, "recovers missing syntax", RecoversMissingSyntax);
        Run(failures, "binds queryable symbols and types", BindsSymbols);
        Run(failures, "reports unknown names", ReportsUnknownName);
        Run(failures, "emits deterministic PE and portable PDB", EmitsManagedArtifacts);
        Run(failures, "executes emitted display program", ExecutesEmittedProgram);
        if (failures.Count == 0) { Console.WriteLine("All COBOL compiler tests passed."); return 0; }
        Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
        return 1;
    }

    private static void BuildsCompleteTree()
    {
        var tree = SyntaxTree.Parse(new SourceText(TestProgram.Source));
        TestAssert.True(tree.Diagnostics.IsEmpty, string.Join(Environment.NewLine, tree.Diagnostics));
        TestAssert.Equal("ACCOUNT", tree.Root.ProgramId?.Identifier.Text);
        TestAssert.Equal(2, tree.Root.DataItems.Length);
        TestAssert.Equal(5, tree.Root.Statements.Length);
        var firstDataItem = tree.Root.DataItems[0];
        TestAssert.True(firstDataItem.Parent is SectionSyntax, "Data item must retain parent.");
        TestAssert.True(firstDataItem.Span.Start >= firstDataItem.FullSpan.Start, "Full span must include or equal the span.");
        TestAssert.True(tree.Tokens.Any(static token => token.LeadingTrivia.Length > 0), "Tokens must preserve trivia.");
        TestAssert.Equal(TestProgram.NormalizedSource, tree.ToFullString());
    }

    private static void RecoversMissingSyntax()
    {
        var tree = SyntaxTree.Parse(new SourceText("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BAD
            PROCEDURE DIVISION.
            STOP RUN.
            """));
        TestAssert.True(tree.Diagnostics.Any(static diagnostic => diagnostic.Descriptor.Id == "COB1001"), "Expected parse diagnostic.");
        TestAssert.True(tree.Root.ProgramId?.PeriodToken.IsMissing == true, "Expected synthesized missing period.");
    }

    private static void BindsSymbols()
    {
        var tree = SyntaxTree.Parse(new SourceText(TestProgram.Source));
        var model = Compilation.Create(tree).GetSemanticModel(tree);
        TestAssert.Equal("ACCOUNT", model.Program?.Name);
        TestAssert.Equal(2, model.Fields.Length);
        var balance = model.GetDeclaredSymbol("balance");
        TestAssert.True(balance is FieldSymbol { Type: var type } && type == TypeSymbol.FixedPoint, "Expected decimal BALANCE symbol.");
        var name = tree.Root.Statements.OfType<DisplayStatementSyntax>().First().Operands[0] as NameExpressionSyntax;
        TestAssert.True(name is not null && model.GetSymbolInfo(name).Symbol is FieldSymbol, "Expected bound name symbol.");
        TestAssert.True(model.GetTypeInfo(name!).Type == TypeSymbol.Alphanumeric, "Expected name type.");
    }

    private static void ReportsUnknownName()
    {
        var tree = SyntaxTree.Parse(new SourceText("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BAD.
            PROCEDURE DIVISION.
            MOVE 1 TO MISSING.
            STOP RUN.
            """));
        TestAssert.True(Compilation.Create(tree).GetDiagnostics().Any(static diagnostic => diagnostic.Descriptor.Id == "COB3001"), "Expected unknown-name diagnostic.");
    }

    private static void EmitsManagedArtifacts()
    {
        var first = Emit();
        var second = Emit();
        TestAssert.True(first.Pe.Length > 0 && first.Pdb.Length > 0, "PE or portable PDB was empty.");
        TestAssert.True(first.Pe.SequenceEqual(second.Pe), "Deterministic emission must produce identical PE bytes.");
        using var peReader = new PEReader(new MemoryStream(first.Pe));
        TestAssert.True(peReader.HasMetadata, "Output was not a managed PE.");
        var metadata = peReader.GetMetadataReader();
        TestAssert.True(!metadata.GetModuleDefinition().Mvid.IsNil, "Managed module MVID is absent.");
        TestAssert.True(!peReader.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress.Equals(0), "Executable entry point is absent.");
        using var pdbReader = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(first.Pdb));
        var pdb = pdbReader.GetMetadataReader();
        TestAssert.True(pdb.Documents.Count > 0, "Portable PDB has no documents.");
        TestAssert.True(pdb.MethodDebugInformation.Any(handle => !pdb.GetMethodDebugInformation(handle).SequencePointsBlob.IsNil), "Portable PDB has no sequence points.");
    }

    private static void ExecutesEmittedProgram()
    {
        var emitted = Emit();
        var assembly = Assembly.Load(emitted.Pe);
        var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("Emitted executable has no entry point.");
        var originalOut = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        try { entryPoint.Invoke(null, null); }
        finally { Console.SetOut(originalOut); }
        TestAssert.True(output.ToString().Contains("ACCOUNT ACTIVE", StringComparison.Ordinal), "Emitted program did not execute expected DISPLAY statement.");
    }

    private static EmittedArtifacts Emit()
    {
        var compilation = Compilation.Create(SyntaxTree.Parse(new SourceText(TestProgram.Source)));
        using var pe = new MemoryStream();
        using var pdb = new MemoryStream();
        var result = compilation.Emit(pe, pdb, new EmitOptions("Account"));
        TestAssert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return new EmittedArtifacts(pe.ToArray(), pdb.ToArray());
    }

    private static void Run(List<string> failures, string name, Action test)
    {
        try { test(); Console.WriteLine($"PASS {name}"); }
        catch (Exception exception) { failures.Add($"FAIL {name}: {exception.Message}"); }
    }
}
