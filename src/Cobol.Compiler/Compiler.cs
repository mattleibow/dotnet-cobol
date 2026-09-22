using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Cobol.Compiler;

/// <summary>Describes the supported first-milestone COBOL source profile.</summary>
public static class CobolProfile
{
    /// <summary>The stable name used in diagnostics and documentation.</summary>
    public const string Name = "DotNetCobol 0.1 core profile";
}

/// <summary>Immutable source text and its logical file name.</summary>
public sealed class SourceText
{
    /// <summary>Creates source text.</summary>
    public SourceText(string text, string filePath = "")
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        FilePath = filePath;
    }

    /// <summary>Gets the source content.</summary>
    public string Text { get; }

    /// <summary>Gets the logical path displayed in diagnostics.</summary>
    public string FilePath { get; }

    /// <summary>Creates source text from a file.</summary>
    public static SourceText FromFile(string path) => new(File.ReadAllText(path), path);

    /// <summary>Gets one-based line and column for an offset.</summary>
    public (int Line, int Column) GetLinePosition(int offset)
    {
        var bounded = Math.Clamp(offset, 0, Text.Length);
        var line = 1;
        var column = 1;
        for (var i = 0; i < bounded; i++)
        {
            if (Text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}

/// <summary>A contiguous source range.</summary>
public readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>Gets the first offset after the range.</summary>
    public int End => Start + Length;
}

/// <summary>A source file and range.</summary>
public readonly record struct Location(SourceText Source, TextSpan Span)
{
    /// <summary>Gets a display string for the location.</summary>
    public override string ToString()
    {
        var (line, column) = Source.GetLinePosition(Span.Start);
        return $"{(string.IsNullOrEmpty(Source.FilePath) ? "<source>" : Source.FilePath)}({line},{column})";
    }
}

/// <summary>Describes a compiler diagnostic.</summary>
public sealed record DiagnosticDescriptor(string Id, string Title, string MessageFormat, DiagnosticSeverity Severity);

/// <summary>Diagnostic severities emitted by the COBOL pipeline.</summary>
public enum DiagnosticSeverity { Info, Warning, Error }

/// <summary>An immutable compiler diagnostic.</summary>
public sealed record Diagnostic(DiagnosticDescriptor Descriptor, Location Location, params object[] Arguments)
{
    /// <summary>Gets the rendered diagnostic message.</summary>
    public string Message => string.Format(CultureInfo.InvariantCulture, Descriptor.MessageFormat, Arguments);

    /// <summary>Gets whether this diagnostic prevents emission.</summary>
    public bool IsError => Descriptor.Severity == DiagnosticSeverity.Error;

    /// <inheritdoc />
    public override string ToString() => $"{Location}: {Descriptor.Severity.ToString().ToLowerInvariant()} {Descriptor.Id}: {Message}";
}

/// <summary>Catalog of stable diagnostic descriptors for the implemented profile.</summary>
public static class Diagnostics
{
    /// <summary>Unexpected syntax.</summary>
    public static readonly DiagnosticDescriptor UnexpectedToken = new("COB1001", "Unexpected token", "Expected {0}, but found '{1}'.", DiagnosticSeverity.Error);
    /// <summary>Unsupported COBOL construct.</summary>
    public static readonly DiagnosticDescriptor UnsupportedConstruct = new("COB2001", "Unsupported construct", "'{0}' is not supported by {1}.", DiagnosticSeverity.Error);
    /// <summary>Missing program identifier.</summary>
    public static readonly DiagnosticDescriptor MissingProgramId = new("COB2002", "Missing PROGRAM-ID", "IDENTIFICATION DIVISION must contain PROGRAM-ID.", DiagnosticSeverity.Error);
    /// <summary>Unknown data name.</summary>
    public static readonly DiagnosticDescriptor UnknownName = new("COB3001", "Unknown data name", "Data item '{0}' was not declared in WORKING-STORAGE.", DiagnosticSeverity.Error);
    /// <summary>Backend compiler failure.</summary>
    public static readonly DiagnosticDescriptor EmitFailure = new("COB9001", "Emission failed", "{0}", DiagnosticSeverity.Error);
}

/// <summary>Syntax element kinds exposed by the immutable syntax API.</summary>
public enum SyntaxKind
{
    /// <summary>End of source.</summary>
    EndOfFileToken,
    /// <summary>An identifier or COBOL keyword.</summary>
    WordToken,
    /// <summary>A numeric literal.</summary>
    NumberToken,
    /// <summary>A quoted literal.</summary>
    StringToken,
    /// <summary>Period separator.</summary>
    PeriodToken,
    /// <summary>Relational or arithmetic operator.</summary>
    OperatorToken,
    /// <summary>Unexpected character.</summary>
    BadToken,
    /// <summary>Compilation root.</summary>
    CompilationUnit,
    /// <summary>Data declaration.</summary>
    DataItem,
    /// <summary>DISPLAY statement.</summary>
    DisplayStatement,
    /// <summary>MOVE statement.</summary>
    MoveStatement,
    /// <summary>ACCEPT statement.</summary>
    AcceptStatement,
    /// <summary>Arithmetic statement.</summary>
    ArithmeticStatement,
    /// <summary>IF statement.</summary>
    IfStatement,
    /// <summary>STOP RUN or GOBACK statement.</summary>
    ExitStatement,
    /// <summary>Unsupported procedure statement.</summary>
    UnsupportedStatement,
}

/// <summary>Whitespace or comment attached to a token.</summary>
public sealed record SyntaxTrivia(string Text, TextSpan Span, bool IsComment);

/// <summary>An immutable lexical token.</summary>
public sealed record SyntaxToken(SyntaxKind Kind, string Text, object? Value, Location Location, ImmutableArray<SyntaxTrivia> LeadingTrivia);

/// <summary>Base class for an immutable syntax node.</summary>
public abstract record SyntaxNode(SyntaxKind Kind, Location Location);

/// <summary>A parsed elementary WORKING-STORAGE data declaration.</summary>
public sealed record DataItemSyntax(
    int Level,
    string Name,
    string Picture,
    string? Value,
    Location Location) : SyntaxNode(SyntaxKind.DataItem, Location);

/// <summary>Base class for parsed procedure statements.</summary>
public abstract record StatementSyntax(SyntaxKind Kind, Location Location) : SyntaxNode(Kind, Location);

/// <summary>A DISPLAY statement.</summary>
public sealed record DisplayStatementSyntax(ImmutableArray<string> Operands, Location Location) : StatementSyntax(SyntaxKind.DisplayStatement, Location);

/// <summary>A MOVE statement.</summary>
public sealed record MoveStatementSyntax(string Source, string Destination, Location Location) : StatementSyntax(SyntaxKind.MoveStatement, Location);

/// <summary>An ACCEPT statement.</summary>
public sealed record AcceptStatementSyntax(string Destination, Location Location) : StatementSyntax(SyntaxKind.AcceptStatement, Location);

/// <summary>An ADD, SUBTRACT, MULTIPLY, or DIVIDE statement.</summary>
public sealed record ArithmeticStatementSyntax(string Operation, string Operand, string Destination, Location Location) : StatementSyntax(SyntaxKind.ArithmeticStatement, Location);

/// <summary>An IF statement with a single statement body.</summary>
public sealed record IfStatementSyntax(string Left, string Operator, string Right, StatementSyntax? ThenStatement, Location Location) : StatementSyntax(SyntaxKind.IfStatement, Location);

/// <summary>A terminating procedure statement.</summary>
public sealed record ExitStatementSyntax(string Keyword, Location Location) : StatementSyntax(SyntaxKind.ExitStatement, Location);

/// <summary>A procedure statement outside the initial profile.</summary>
public sealed record UnsupportedStatementSyntax(string Keyword, Location Location) : StatementSyntax(SyntaxKind.UnsupportedStatement, Location);

/// <summary>The parsed root of a single-program COBOL source file.</summary>
public sealed record CompilationUnitSyntax(
    string? ProgramId,
    ImmutableArray<DataItemSyntax> DataItems,
    ImmutableArray<StatementSyntax> Statements,
    Location Location) : SyntaxNode(SyntaxKind.CompilationUnit, Location);

/// <summary>A source file's immutable syntax tree and parse diagnostics.</summary>
public sealed class SyntaxTree
{
    internal SyntaxTree(SourceText text, CompilationUnitSyntax root, ImmutableArray<SyntaxToken> tokens, ImmutableArray<Diagnostic> diagnostics)
    {
        Text = text;
        Root = root;
        Tokens = tokens;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets source text.</summary>
    public SourceText Text { get; }
    /// <summary>Gets the parsed root.</summary>
    public CompilationUnitSyntax Root { get; }
    /// <summary>Gets all tokens including end-of-file.</summary>
    public ImmutableArray<SyntaxToken> Tokens { get; }
    /// <summary>Gets lexical and parsing diagnostics.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>Parses one COBOL source file.</summary>
    public static SyntaxTree Parse(SourceText source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Lex();
        var parser = new Parser(source, tokens, lexer.Diagnostics);
        return parser.Parse();
    }
}

internal sealed class Lexer
{
    private readonly SourceText _source;
    private readonly List<Diagnostic> _diagnostics = [];
    private readonly List<SyntaxToken> _tokens = [];

    public Lexer(SourceText source) => _source = new SourceText(Normalize(source.Text), source.FilePath);
    public ImmutableArray<Diagnostic> Diagnostics => [.. _diagnostics];

    public ImmutableArray<SyntaxToken> Lex()
    {
        var text = _source.Text;
        var position = 0;
        while (position < text.Length)
        {
            var triviaStart = position;
            var trivia = new List<SyntaxTrivia>();
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                var start = position++;
                while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
                trivia.Add(new SyntaxTrivia(text[start..position], new TextSpan(start, position - start), false));
            }

            if (position >= text.Length) break;
            var startToken = position;
            SyntaxKind kind;
            object? value = null;
            if (char.IsLetter(text[position]) || text[position] is '_' or '-')
            {
                position++;
                while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] is '_' or '-')) position++;
                kind = SyntaxKind.WordToken;
            }
            else if (char.IsDigit(text[position]))
            {
                position++;
                var seenDecimalPoint = false;
                while (position < text.Length &&
                       (char.IsDigit(text[position]) ||
                        (!seenDecimalPoint && text[position] == '.' && position + 1 < text.Length && char.IsDigit(text[position + 1]))))
                {
                    seenDecimalPoint |= text[position] == '.';
                    position++;
                }
                kind = SyntaxKind.NumberToken;
                value = decimal.Parse(text[startToken..position], CultureInfo.InvariantCulture);
            }
            else if (text[position] is '\'' or '"')
            {
                var quote = text[position++];
                while (position < text.Length && text[position] != quote) position++;
                if (position < text.Length) position++;
                kind = SyntaxKind.StringToken;
                value = text[(startToken + 1)..Math.Max(startToken + 1, position - 1)];
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

            var tokenText = text[startToken..position];
            _tokens.Add(new SyntaxToken(kind, tokenText, value, new Location(_source, new TextSpan(startToken, position - startToken)), [.. trivia]));
        }

        var eof = new Location(_source, new TextSpan(text.Length, 0));
        _tokens.Add(new SyntaxToken(SyntaxKind.EndOfFileToken, string.Empty, null, eof, []));
        return [.. _tokens];
    }

    private static string Normalize(string input)
    {
        var lines = input.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var free = lines.Any(static line => line.TrimStart().StartsWith(">>SOURCE FORMAT FREE", StringComparison.OrdinalIgnoreCase));
        if (free) return string.Join('\n', lines.Where(static line => !line.TrimStart().StartsWith(">>SOURCE", StringComparison.OrdinalIgnoreCase)));
        var fixedFormat = lines.Any(static line => line.Length > 7 && line.AsSpan(0, 7).Trim().IsEmpty);
        if (!fixedFormat) return string.Join('\n', lines);

        var normalized = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            if (line.Length >= 7 && line[6] is '*' or '/') continue;
            normalized.Add(line.Length > 6 ? line[6..Math.Min(line.Length, 72)] : line);
        }

        return string.Join('\n', normalized);
    }
}

internal sealed class Parser
{
    private readonly SourceText _source;
    private readonly ImmutableArray<SyntaxToken> _tokens;
    private readonly List<Diagnostic> _diagnostics;
    private int _position;

    public Parser(SourceText source, ImmutableArray<SyntaxToken> tokens, ImmutableArray<Diagnostic> diagnostics)
    {
        _source = source;
        _tokens = tokens;
        _diagnostics = [.. diagnostics];
    }

    public SyntaxTree Parse()
    {
        string? programId = null;
        var dataItems = ImmutableArray.CreateBuilder<DataItemSyntax>();
        var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
        var section = string.Empty;

        while (Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (IsDivision("IDENTIFICATION")) { section = "IDENTIFICATION"; SkipDivision(); continue; }
            if (IsDivision("ENVIRONMENT")) { section = "ENVIRONMENT"; SkipDivision(); continue; }
            if (IsDivision("DATA")) { section = "DATA"; SkipDivision(); continue; }
            if (IsDivision("PROCEDURE")) { section = "PROCEDURE"; SkipDivision(); continue; }
            if (section == "IDENTIFICATION" && IsWord("PROGRAM-ID"))
            {
                NextToken(); Match(SyntaxKind.PeriodToken);
                programId = MatchWord().Text;
                Match(SyntaxKind.PeriodToken);
                continue;
            }

            if (section == "DATA" && IsWord("WORKING-STORAGE")) { NextToken(); if (Current.Kind == SyntaxKind.WordToken) NextToken(); Match(SyntaxKind.PeriodToken); continue; }
            if (section == "DATA" && Current.Kind == SyntaxKind.NumberToken) { dataItems.Add(ParseDataItem()); continue; }
            if (section == "PROCEDURE")
            {
                if (Current.Kind == SyntaxKind.WordToken && Peek(1).Kind == SyntaxKind.PeriodToken) { NextToken(); NextToken(); continue; }
                statements.Add(ParseStatement());
                continue;
            }

            NextToken();
        }

        if (string.IsNullOrWhiteSpace(programId))
            _diagnostics.Add(new Diagnostic(Diagnostics.MissingProgramId, new Location(_source, new TextSpan(0, 0))));

        var root = new CompilationUnitSyntax(programId, dataItems.ToImmutable(), statements.ToImmutable(), new Location(_source, new TextSpan(0, _source.Text.Length)));
        return CreateTree(root);
    }

    private SyntaxTree CreateTree(CompilationUnitSyntax root) => new(_source, root, _tokens, _diagnostics.ToImmutableArray());

    private DataItemSyntax ParseDataItem()
    {
        var levelToken = NextToken();
        var level = Convert.ToInt32(levelToken.Value, CultureInfo.InvariantCulture);
        var name = MatchWord();
        if (!IsWord("PIC") && !IsWord("PICTURE"))
        {
            _diagnostics.Add(new Diagnostic(Diagnostics.UnsupportedConstruct, name.Location, "group data item", CobolProfile.Name));
            SkipToPeriod();
            return new DataItemSyntax(level, name.Text, string.Empty, null, name.Location);
        }

        NextToken();
        var pictureParts = new StringBuilder();
        while (Current.Kind is not SyntaxKind.PeriodToken and not SyntaxKind.EndOfFileToken && !IsWord("VALUE"))
        {
            pictureParts.Append(NextToken().Text);
        }

        var picture = pictureParts.ToString();
        if (picture.Length == 0)
        {
            _diagnostics.Add(new Diagnostic(Diagnostics.UnexpectedToken, Current.Location, "PIC clause", Current.Text));
        }

        string? value = null;
        if (IsWord("VALUE"))
        {
            NextToken();
            value = NextToken().Text;
        }

        Match(SyntaxKind.PeriodToken);
        return new DataItemSyntax(level, name.Text, picture, value, levelToken.Location);
    }

    private StatementSyntax ParseStatement()
    {
        var first = MatchWord();
        if (EqualsWord(first, "DISPLAY"))
        {
            var operands = ImmutableArray.CreateBuilder<string>();
            while (Current.Kind is not SyntaxKind.PeriodToken and not SyntaxKind.EndOfFileToken) operands.Add(NextToken().Text);
            Match(SyntaxKind.PeriodToken);
            return new DisplayStatementSyntax(operands.ToImmutable(), first.Location);
        }

        if (EqualsWord(first, "MOVE"))
        {
            var source = NextToken().Text;
            if (!IsWord("TO")) _diagnostics.Add(new Diagnostic(Diagnostics.UnexpectedToken, Current.Location, "TO", Current.Text));
            else NextToken();
            var destination = MatchWord().Text;
            Match(SyntaxKind.PeriodToken);
            return new MoveStatementSyntax(source, destination, first.Location);
        }

        if (EqualsWord(first, "ACCEPT"))
        {
            var destination = MatchWord().Text;
            Match(SyntaxKind.PeriodToken);
            return new AcceptStatementSyntax(destination, first.Location);
        }

        if (EqualsWord(first, "ADD") || EqualsWord(first, "SUBTRACT") || EqualsWord(first, "MULTIPLY") || EqualsWord(first, "DIVIDE"))
        {
            var operand = NextToken().Text;
            if (IsWord("TO") || IsWord("FROM") || IsWord("BY")) NextToken();
            var destination = MatchWord().Text;
            Match(SyntaxKind.PeriodToken);
            return new ArithmeticStatementSyntax(first.Text, operand, destination, first.Location);
        }

        if (EqualsWord(first, "IF"))
        {
            var left = NextToken().Text;
            var op = NextToken().Text;
            var right = NextToken().Text;
            if (IsWord("THEN")) NextToken();
            var body = Current.Kind == SyntaxKind.WordToken ? ParseStatement() : null;
            return new IfStatementSyntax(left, op, right, body, first.Location);
        }

        if (EqualsWord(first, "STOP"))
        {
            if (IsWord("RUN")) NextToken();
            Match(SyntaxKind.PeriodToken);
            return new ExitStatementSyntax("STOP RUN", first.Location);
        }

        if (EqualsWord(first, "GOBACK"))
        {
            Match(SyntaxKind.PeriodToken);
            return new ExitStatementSyntax("GOBACK", first.Location);
        }

        SkipToPeriod();
        return new UnsupportedStatementSyntax(first.Text, first.Location);
    }

    private bool IsDivision(string name) => IsWord(name) && Peek(1).Text.Equals("DIVISION", StringComparison.OrdinalIgnoreCase);
    private void SkipDivision() { NextToken(); NextToken(); Match(SyntaxKind.PeriodToken); }
    private bool IsWord(string text) => Current.Kind == SyntaxKind.WordToken && Current.Text.Equals(text, StringComparison.OrdinalIgnoreCase);
    private static bool EqualsWord(SyntaxToken token, string text) => token.Text.Equals(text, StringComparison.OrdinalIgnoreCase);
    private SyntaxToken MatchWord() => Current.Kind == SyntaxKind.WordToken ? NextToken() : Match(SyntaxKind.WordToken);
    private SyntaxToken Match(SyntaxKind kind)
    {
        if (Current.Kind == kind) return NextToken();
        _diagnostics.Add(new Diagnostic(Diagnostics.UnexpectedToken, Current.Location, kind.ToString(), Current.Text));
        return new SyntaxToken(kind, string.Empty, null, Current.Location, []);
    }

    private void SkipToPeriod() { while (Current.Kind is not SyntaxKind.PeriodToken and not SyntaxKind.EndOfFileToken) NextToken(); Match(SyntaxKind.PeriodToken); }
    private SyntaxToken Current => Peek(0);
    private SyntaxToken Peek(int offset) => _tokens[Math.Min(_position + offset, _tokens.Length - 1)];
    private SyntaxToken NextToken() => _tokens[_position++];
}
