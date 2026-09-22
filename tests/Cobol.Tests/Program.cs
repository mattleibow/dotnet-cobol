using Cobol.Compiler;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var failures = new List<string>();
Run("parses supported divisions and statements", ParsesSupportedProgram);
Run("reports unknown names", ReportsUnknownName);
Run("emits PE and portable PDB", EmitsManagedArtifacts);
Run("executes emitted display program", ExecutesEmittedProgram);

if (failures.Count != 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}

Console.WriteLine("All COBOL compiler tests passed.");
return 0;

void Run(string name, Action test)
{
    try { test(); Console.WriteLine($"PASS {name}"); }
    catch (Exception exception) { failures.Add($"FAIL {name}: {exception.Message}"); }
}

void ParsesSupportedProgram()
{
    var tree = SyntaxTree.Parse(new SourceText(GetSample()));
    Assert(tree.Diagnostics.IsEmpty, string.Join(Environment.NewLine, tree.Diagnostics));
    Assert(tree.Root.ProgramId == "ACCOUNT", "Program ID was not parsed.");
    Assert(tree.Root.DataItems.Length == 2, "Working-storage data items were not parsed.");
    Assert(tree.Root.Statements.Length == 4, "Procedure statements were not parsed.");
}

void ReportsUnknownName()
{
    var tree = SyntaxTree.Parse(new SourceText("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. BAD.
        PROCEDURE DIVISION.
            MOVE 1 TO MISSING.
            STOP RUN.
        """));
    var diagnostics = Compilation.Create(tree).GetDiagnostics();
    Assert(diagnostics.Any(static diagnostic => diagnostic.Descriptor.Id == "COB3001"), "Expected an unknown-name diagnostic.");
}

void EmitsManagedArtifacts()
{
    var tree = SyntaxTree.Parse(new SourceText(GetSample()));
    var compilation = Compilation.Create(tree);
    using var pe = new MemoryStream();
    using var pdb = new MemoryStream();
    var result = compilation.Emit(pe, pdb, new EmitOptions("Account"));
    Assert(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
    Assert(pe.Length > 0 && pdb.Length > 0, "PE or portable PDB was empty.");
    pe.Position = 0;
    using var reader = new PEReader(pe);
    Assert(reader.HasMetadata, "Output was not a managed PE.");
    pdb.Position = 0;
    using var pdbReader = MetadataReaderProvider.FromPortablePdbStream(pdb);
    Assert(pdbReader.GetMetadataReader().Documents.Count > 0, "Portable PDB did not contain source documents.");
}

void ExecutesEmittedProgram()
{
    var tree = SyntaxTree.Parse(new SourceText(GetSample()));
    using var pe = new MemoryStream();
    using var pdb = new MemoryStream();
    var result = Compilation.Create(tree).Emit(pe, pdb, new EmitOptions("ExecutableAccount"));
    Assert(result.Success, string.Join(Environment.NewLine, result.Diagnostics));

    var assembly = Assembly.Load(pe.ToArray());
    var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("Emitted executable did not have an entry point.");
    var originalOut = Console.Out;
    using var output = new StringWriter();
    Console.SetOut(output);
    try
    {
        entryPoint.Invoke(null, null);
    }
    finally
    {
        Console.SetOut(originalOut);
    }

    Assert(output.ToString().Contains("ACCOUNT ACTIVE", StringComparison.Ordinal), "Emitted program did not execute expected DISPLAY statement.");
}

void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

string GetSample() =>
    """
    >>SOURCE FORMAT FREE
    IDENTIFICATION DIVISION.
    PROGRAM-ID. ACCOUNT.
    ENVIRONMENT DIVISION.
    CONFIGURATION SECTION.
    DATA DIVISION.
    WORKING-STORAGE SECTION.
    01 BALANCE PIC 9(5)V99 VALUE 125.50.
    01 OWNER PIC X(20) VALUE 'Ada'.
    PROCEDURE DIVISION.
        ADD 10 TO BALANCE.
        DISPLAY OWNER BALANCE.
        IF BALANCE > 0 DISPLAY 'ACCOUNT ACTIVE'.
        STOP RUN.
    """;
