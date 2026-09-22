# DotNetCobol

DotNetCobol is an experimental, MIT-licensed COBOL language implementation for modern .NET. Its first milestone provides a runnable compiler vertical slice and deliberately **does not** claim ISO COBOL, enterprise-dialect, or Roslyn-equivalent compliance.

## Quick start

```sh
dotnet build DotNetCobol.sln --configuration Release
dotnet run --project src/Cobol.Cli -- samples/account/account.cbl -o account.dll
```

The compiler writes a managed assembly and a portable PDB. The current bootstrap emitter uses Roslyn's supported `CSharpCompilation.Emit` API behind the public `Compilation.Emit` abstraction; it is an implementation detail scheduled for replacement by a direct metadata/IL backend.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/Cobol.Compiler` | Immutable source, syntax, diagnostics, binding, lowering, compilation, and emission APIs |
| `src/Cobol.Cli` | `cobolc` command-line compiler |
| `src/Cobol.MSBuild` | Initial MSBuild task and importable targets |
| `editor/vscode-cobol` | VS Code language extension scaffold |
| `samples` | Small business-oriented example programs |
| `tests/Cobol.Tests` | Dependency-free parser, diagnostic, managed-PE, and portable-PDB validation |
| `docs` | Architecture, profile, standards bibliography, and roadmap |

Read [the supported profile](docs/profile.md) before using the compiler, then [the architecture](docs/architecture.md) and [compliance roadmap](docs/roadmap.md).

`samples/msbuild-project/Account.cobolproj` shows the MSBuild task with a development-time task-assembly override. A packaged `Cobol.MSBuild` package uses the included `build/Cobol.MSBuild.targets` without that override.