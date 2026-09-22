using Cobol.Compiler;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cobol.MSBuild;

/// <summary>Compiles a single <c>.cob</c> or <c>.cbl</c> item into a managed assembly.</summary>
public sealed class CobolCompile : Microsoft.Build.Utilities.Task
{
    /// <summary>Gets or sets the COBOL source path.</summary>
    [Required]
    public required string Source { get; set; }

    /// <summary>Gets or sets the PE output path.</summary>
    [Required]
    public required string OutputAssembly { get; set; }

    /// <summary>Gets or sets whether to emit a class library.</summary>
    public bool OutputLibrary { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        var tree = SyntaxTree.Parse(SourceText.FromFile(Source));
        var compilation = Compilation.Create(tree);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputAssembly))!);
        using var pe = File.Create(OutputAssembly);
        using var pdb = File.Create(Path.ChangeExtension(OutputAssembly, ".pdb"));
        var result = compilation.Emit(pe, pdb, new EmitOptions(Path.GetFileNameWithoutExtension(OutputAssembly), OutputLibrary));
        foreach (var diagnostic in result.Diagnostics)
        {
            if (diagnostic.IsError)
            {
                Log.LogError(diagnostic.ToString());
            }
            else
            {
                Log.LogMessage(MessageImportance.Normal, diagnostic.ToString());
            }
        }

        return result.Success;
    }
}
