using System.Collections.Immutable;

namespace Cobol.Compiler;

internal sealed class Scope
{
    private readonly ImmutableDictionary<string, Symbol> _symbols;
    public Scope(ImmutableDictionary<string, Symbol> symbols) => _symbols = symbols;
    public bool TryLookup(string name, out Symbol? symbol) => _symbols.TryGetValue(name, out symbol);
}
