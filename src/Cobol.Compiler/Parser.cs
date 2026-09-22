using System.Collections.Immutable;

namespace Cobol.Compiler;

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
        var divisions = ImmutableArray.CreateBuilder<DivisionSyntax>();
        while (Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (IsDivisionStart()) divisions.Add(ParseDivision());
            else NextToken();
        }

        var root = new CompilationUnitSyntax(divisions.ToImmutable(), NextToken());
        if (root.ProgramId is null) _diagnostics.Add(new Diagnostic(Diagnostics.MissingProgramId, new Location(_source, new TextSpan(0, 0))));
        return new SyntaxTree(_source, root, _tokens, _diagnostics.ToImmutableArray());
    }

    private DivisionSyntax ParseDivision()
    {
        var name = NextToken();
        var division = MatchWord("DIVISION");
        var period = Match(SyntaxKind.PeriodToken);
        var members = ImmutableArray.CreateBuilder<SyntaxNode>();
        if (IsWord(name, "IDENTIFICATION"))
        {
            while (!IsDivisionStart() && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (IsWord("PROGRAM-ID")) members.Add(ParseProgramId());
                else NextToken();
            }
        }
        else if (IsWord(name, "DATA"))
        {
            while (!IsDivisionStart() && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (IsSectionStart()) members.Add(ParseSection(ParseDataMembers));
                else if (Current.Kind == SyntaxKind.NumberToken) members.Add(ParseDataItem());
                else NextToken();
            }
        }
        else if (IsWord(name, "PROCEDURE"))
        {
            while (!IsDivisionStart() && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (IsSectionStart()) members.Add(ParseSection(ParseProcedureMembers));
                else if (IsParagraphStart()) members.Add(ParseParagraph());
                else if (Current.Kind == SyntaxKind.WordToken) members.Add(ParseStatement());
                else NextToken();
            }
        }
        else
        {
            while (!IsDivisionStart() && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (IsSectionStart()) members.Add(ParseSection(static () => []));
                else NextToken();
            }
        }

        return new DivisionSyntax(name, division, period, members.ToImmutable());
    }

    private SectionSyntax ParseSection(Func<ImmutableArray<SyntaxNode>> readMembers)
    {
        var name = NextToken();
        var section = MatchWord("SECTION");
        var period = Match(SyntaxKind.PeriodToken);
        return new SectionSyntax(name, section, period, readMembers());
    }

    private ImmutableArray<SyntaxNode> ParseDataMembers()
    {
        var members = ImmutableArray.CreateBuilder<SyntaxNode>();
        while (!IsDivisionStart() && !IsSectionStart() && Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (Current.Kind == SyntaxKind.NumberToken) members.Add(ParseDataItem());
            else NextToken();
        }

        return members.ToImmutable();
    }

    private ImmutableArray<SyntaxNode> ParseProcedureMembers()
    {
        var members = ImmutableArray.CreateBuilder<SyntaxNode>();
        while (!IsDivisionStart() && !IsSectionStart() && Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (IsParagraphStart()) members.Add(ParseParagraph());
            else if (Current.Kind == SyntaxKind.WordToken) members.Add(ParseStatement());
            else NextToken();
        }

        return members.ToImmutable();
    }

    private ProgramIdSyntax ParseProgramId()
    {
        var keyword = NextToken();
        var firstPeriod = Match(SyntaxKind.PeriodToken);
        var identifier = Match(SyntaxKind.WordToken);
        return new ProgramIdSyntax(keyword, firstPeriod, identifier, Match(SyntaxKind.PeriodToken));
    }

    private DataItemSyntax ParseDataItem()
    {
        var level = NextToken();
        var identifier = Match(SyntaxKind.WordToken);
        SyntaxToken? pictureKeyword = null;
        var picture = ImmutableArray.CreateBuilder<SyntaxToken>();
        if (IsWord("PIC") || IsWord("PICTURE"))
        {
            pictureKeyword = NextToken();
            while (Current.Kind is not SyntaxKind.PeriodToken and not SyntaxKind.EndOfFileToken && !IsWord("VALUE")) picture.Add(NextToken());
        }
        else
        {
            _diagnostics.Add(new Diagnostic(Diagnostics.UnsupportedConstruct, identifier.Location, "group data item", CobolProfile.Name));
        }

        SyntaxToken? valueKeyword = null;
        ExpressionSyntax? value = null;
        if (IsWord("VALUE")) { valueKeyword = NextToken(); value = ParseExpression(); }
        return new DataItemSyntax(level, identifier, pictureKeyword, picture.ToImmutable(), valueKeyword, value, Match(SyntaxKind.PeriodToken));
    }

    private ParagraphSyntax ParseParagraph()
    {
        var identifier = NextToken();
        var period = NextToken();
        var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
        while (!IsDivisionStart() && !IsSectionStart() && !IsParagraphStart() && Current.Kind != SyntaxKind.EndOfFileToken)
        {
            if (Current.Kind == SyntaxKind.WordToken) statements.Add(ParseStatement());
            else NextToken();
        }

        return new ParagraphSyntax(identifier, period, statements.ToImmutable());
    }

    private StatementSyntax ParseStatement()
    {
        var keyword = NextToken();
        if (IsWord(keyword, "DISPLAY"))
        {
            var operands = ImmutableArray.CreateBuilder<ExpressionSyntax>();
            while (Current.Kind is not SyntaxKind.PeriodToken and not SyntaxKind.EndOfFileToken) operands.Add(ParseExpression());
            return new DisplayStatementSyntax(keyword, operands.ToImmutable(), Match(SyntaxKind.PeriodToken));
        }

        if (IsWord(keyword, "MOVE")) return new MoveStatementSyntax(keyword, ParseExpression(), MatchWord("TO"), ParseName(), Match(SyntaxKind.PeriodToken));
        if (IsWord(keyword, "ACCEPT")) return new AcceptStatementSyntax(keyword, ParseName(), Match(SyntaxKind.PeriodToken));
        if (IsWord(keyword, "ADD") || IsWord(keyword, "SUBTRACT") || IsWord(keyword, "MULTIPLY") || IsWord(keyword, "DIVIDE"))
            return new ArithmeticStatementSyntax(keyword, ParseExpression(), MatchWord(IsWord(keyword, "ADD") ? "TO" : IsWord(keyword, "SUBTRACT") ? "FROM" : "BY"), ParseName(), Match(SyntaxKind.PeriodToken));
        if (IsWord(keyword, "IF"))
        {
            var left = ParseExpression();
            var op = Match(SyntaxKind.OperatorToken);
            var right = ParseExpression();
            SyntaxToken? then = IsWord("THEN") ? NextToken() : null;
            return new IfStatementSyntax(keyword, left, op, right, then, Current.Kind == SyntaxKind.WordToken ? ParseStatement() : null);
        }

        if (IsWord(keyword, "STOP")) return new ExitStatementSyntax(keyword, IsWord("RUN") ? NextToken() : null, Match(SyntaxKind.PeriodToken));
        if (IsWord(keyword, "GOBACK")) return new ExitStatementSyntax(keyword, null, Match(SyntaxKind.PeriodToken));
        var skipped = ImmutableArray.CreateBuilder<SyntaxToken>();
        skipped.Add(keyword);
        while (Current.Kind is not SyntaxKind.PeriodToken and not SyntaxKind.EndOfFileToken) skipped.Add(NextToken());
        skipped.Add(Match(SyntaxKind.PeriodToken));
        return new UnsupportedStatementSyntax(skipped.ToImmutable());
    }

    private ExpressionSyntax ParseExpression() => Current.Kind is SyntaxKind.NumberToken or SyntaxKind.StringToken ? new LiteralExpressionSyntax(NextToken()) : ParseName();
    private NameExpressionSyntax ParseName() => new(Match(SyntaxKind.WordToken));
    private bool IsDivisionStart() => Current.Kind == SyntaxKind.WordToken && Peek(1).Kind == SyntaxKind.WordToken && IsWord(Peek(1), "DIVISION");
    private bool IsSectionStart() => Current.Kind == SyntaxKind.WordToken && Peek(1).Kind == SyntaxKind.WordToken && IsWord(Peek(1), "SECTION");
    private bool IsParagraphStart() => Current.Kind == SyntaxKind.WordToken && Peek(1).Kind == SyntaxKind.PeriodToken && !IsWord("STOP") && !IsWord("GOBACK");
    private bool IsWord(string text) => IsWord(Current, text);
    private static bool IsWord(SyntaxToken token, string text) => token.Kind == SyntaxKind.WordToken && token.Text.Equals(text, StringComparison.OrdinalIgnoreCase);
    private SyntaxToken MatchWord(string expected) => IsWord(expected) ? NextToken() : Missing(SyntaxKind.WordToken, expected);
    private SyntaxToken Match(SyntaxKind expected) => Current.Kind == expected ? NextToken() : Missing(expected, expected.ToString());
    private SyntaxToken Missing(SyntaxKind kind, string expected)
    {
        _diagnostics.Add(new Diagnostic(Diagnostics.UnexpectedToken, Current.Location, expected, Current.Text));
        return new SyntaxToken(SyntaxKind.MissingToken, string.Empty, null, new Location(_source, new TextSpan(Current.Span.Start, 0)), [], true);
    }

    private SyntaxToken Current => Peek(0);
    private SyntaxToken Peek(int offset) => _tokens[Math.Min(_position + offset, _tokens.Length - 1)];
    private SyntaxToken NextToken() => _tokens[_position++];
}
