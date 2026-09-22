using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>Returns output status and immutable diagnostics from emission.</summary>
public sealed record EmitResult(bool Success, ImmutableArray<Diagnostic> Diagnostics);
