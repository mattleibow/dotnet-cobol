using System.Collections.Immutable;
using System.Globalization;

namespace Cobol.Compiler;

internal sealed class Lexer
{
    private readonly SourceText _source;
    private readonly List<Diagnostic> _diagnostics = [];

    public Lexer(SourceText source, ParseOptions options) => _source = new SourceText(Normalize(source.Text, options), source.FilePath);

    public ImmutableArray<Diagnostic> Diagnostics => [.. _diagnostics];

    public ImmutableArray<SyntaxToken> Lex()
    {
        var tokens = ImmutableArray.CreateBuilder<SyntaxToken>();
        var text = _source.Text;
        var position = 0;
        while (position < text.Length)
        {
            var trivia = ReadTrivia(text, ref position);
            if (position >= text.Length) break;
            var start = position;
            SyntaxKind kind;
            object? value = null;
            if (char.IsLetter(text[position]) || text[position] is '_' or '-')
            {
                while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] is '_' or '-')) position++;
                kind = SyntaxKind.WordToken;
            }
            else if (char.IsDigit(text[position]))
            {
                var seenDecimalPoint = false;
                while (position < text.Length && (char.IsDigit(text[position]) || (!seenDecimalPoint && text[position] == '.' && position + 1 < text.Length && char.IsDigit(text[position + 1]))))
                {
                    seenDecimalPoint |= text[position] == '.';
                    position++;
                }

                kind = SyntaxKind.NumberToken;
                value = decimal.Parse(text[start..position], CultureInfo.InvariantCulture);
            }
            else if (text[position] is '\'' or '"')
            {
                var quote = text[position++];
                while (position < text.Length && text[position] != quote) position++;
                if (position < text.Length) position++;
                kind = SyntaxKind.StringToken;
                value = text[(start + 1)..Math.Max(start + 1, position - 1)];
            }
            else if (text[position] == '.')
            {
                position++;
                kind = SyntaxKind.PeriodToken;
            }
            else if ("+-*/=<>()".Contains(text[position], StringComparison.Ordinal))
            {
                position++;
                if (position < text.Length && text[position - 1] is '<' or '>' && text[position] == '=') position++;
                kind = SyntaxKind.OperatorToken;
            }
            else
            {
                position++;
                kind = SyntaxKind.BadToken;
            }

            tokens.Add(new SyntaxToken(kind, text[start..position], value, new Location(_source, new TextSpan(start, position - start)), trivia));
        }

        tokens.Add(new SyntaxToken(SyntaxKind.EndOfFileToken, string.Empty, null, new Location(_source, new TextSpan(text.Length, 0)), []));
        return tokens.ToImmutable();
    }

    private static ImmutableArray<SyntaxTrivia> ReadTrivia(string text, ref int position)
    {
        var trivia = ImmutableArray.CreateBuilder<SyntaxTrivia>();
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            var start = position++;
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
            trivia.Add(new SyntaxTrivia(text[start..position], new TextSpan(start, position - start), false));
        }

        return trivia.ToImmutable();
    }

    private static string Normalize(string input, ParseOptions options)
    {
        var lines = input.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var isFree = !options.PreferFixedFormat && lines.Any(static line => line.TrimStart().StartsWith(">>SOURCE FORMAT FREE", StringComparison.OrdinalIgnoreCase));
        if (isFree) return string.Join('\n', lines.Where(static line => !line.TrimStart().StartsWith(">>SOURCE", StringComparison.OrdinalIgnoreCase)));
        var isFixed = options.PreferFixedFormat || lines.Any(static line => line.Length > 7 && line.AsSpan(0, 7).Trim().IsEmpty);
        if (!isFixed) return string.Join('\n', lines);
        return string.Join('\n', lines.Where(static line => line.Length < 7 || line[6] is not '*' and not '/').Select(static line => line.Length > 6 ? line[6..Math.Min(line.Length, 72)] : line));
    }
}
