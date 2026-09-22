using System.Globalization;

namespace Cobol.Compiler;

/// <summary>An immutable compiler diagnostic with a source location.</summary>
public sealed record Diagnostic(DiagnosticDescriptor Descriptor, Location Location, params object[] Arguments)
{
    /// <summary>Gets the invariant-culture rendered message.</summary>
    public string Message => string.Format(CultureInfo.InvariantCulture, Descriptor.MessageFormat, Arguments);

    /// <summary>Gets whether this diagnostic prevents emission.</summary>
    public bool IsError => Descriptor.Severity == DiagnosticSeverity.Error;

    /// <inheritdoc />
    public override string ToString() => $"{Location}: {Descriptor.Severity.ToString().ToLowerInvariant()} {Descriptor.Id}: {Message}";
}
