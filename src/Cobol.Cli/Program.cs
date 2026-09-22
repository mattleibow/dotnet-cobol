using Cobol.Compiler;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("Usage: cobolc <input.cob|input.cbl> [-o <output.dll|output.exe>] [--library]");
    return args.Length == 0 ? 1 : 0;
}

var input = args[0];
if (!File.Exists(input))
{
    Console.Error.WriteLine($"cobolc: source file not found: {input}");
    return 1;
}

var library = args.Contains("--library", StringComparer.OrdinalIgnoreCase);
var outputIndex = Array.FindIndex(args, static argument => argument is "-o" or "--output");
var output = outputIndex >= 0 && outputIndex + 1 < args.Length
    ? args[outputIndex + 1]
    : Path.ChangeExtension(input, library ? ".dll" : ".exe");

var tree = SyntaxTree.Parse(SourceText.FromFile(input));
var compilation = Compilation.Create(tree);
await using var pe = File.Create(output);
await using var pdb = File.Create(Path.ChangeExtension(output, ".pdb"));
var result = compilation.Emit(pe, pdb, new EmitOptions(Path.GetFileNameWithoutExtension(output), library));
foreach (var diagnostic in result.Diagnostics) Console.Error.WriteLine(diagnostic);
return result.Success ? 0 : 1;
