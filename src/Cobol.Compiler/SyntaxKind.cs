namespace Cobol.Compiler;

/// <summary>Identifies a syntax token or node kind.</summary>
public enum SyntaxKind
{
    EndOfFileToken, WordToken, NumberToken, StringToken, PeriodToken, OperatorToken, BadToken, MissingToken,
    CompilationUnit, Division, Section, ProgramId, DataItem, Paragraph,
    LiteralExpression, NameExpression, DisplayStatement, MoveStatement, AcceptStatement,
    ArithmeticStatement, IfStatement, ExitStatement, UnsupportedStatement,
}
