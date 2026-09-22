using System.Collections.Immutable;

namespace Cobol.Compiler;

/// <summary>An elementary WORKING-STORAGE data declaration.</summary>
public sealed class DataItemSyntax : SyntaxNode
{
    internal DataItemSyntax(SyntaxToken level, SyntaxToken identifier, SyntaxToken? pictureKeyword, ImmutableArray<SyntaxToken> pictureTokens, SyntaxToken? valueKeyword, ExpressionSyntax? value, SyntaxToken period)
        : base(SyntaxKind.DataItem, value is null ? [] : [value], BuildTokens(level, identifier, pictureKeyword, pictureTokens, valueKeyword, period))
    {
        LevelToken = level; Identifier = identifier; PictureKeyword = pictureKeyword; PictureTokens = pictureTokens; ValueKeyword = valueKeyword; Value = value; PeriodToken = period;
    }

    /// <summary>Gets the level number token.</summary>
    public SyntaxToken LevelToken { get; }
    /// <summary>Gets the declared data-name.</summary>
    public SyntaxToken Identifier { get; }
    /// <summary>Gets the PIC or PICTURE keyword.</summary>
    public SyntaxToken? PictureKeyword { get; }
    /// <summary>Gets picture-clause tokens.</summary>
    public ImmutableArray<SyntaxToken> PictureTokens { get; }
    /// <summary>Gets the optional VALUE keyword.</summary>
    public SyntaxToken? ValueKeyword { get; }
    /// <summary>Gets the optional initial value.</summary>
    public ExpressionSyntax? Value { get; }
    /// <summary>Gets the terminating period.</summary>
    public SyntaxToken PeriodToken { get; }
    /// <summary>Gets the parsed numeric level, or zero for recovery syntax.</summary>
    public int Level => LevelToken.Value is decimal value ? decimal.ToInt32(value) : 0;
    /// <summary>Gets picture text.</summary>
    public string Picture => string.Concat(PictureTokens.Select(static token => token.Text));

    private static ImmutableArray<SyntaxToken> BuildTokens(SyntaxToken level, SyntaxToken identifier, SyntaxToken? pictureKeyword, ImmutableArray<SyntaxToken> pictureTokens, SyntaxToken? valueKeyword, SyntaxToken period)
    {
        var builder = ImmutableArray.CreateBuilder<SyntaxToken>();
        builder.Add(level); builder.Add(identifier); if (pictureKeyword is not null) builder.Add(pictureKeyword); builder.AddRange(pictureTokens); if (valueKeyword is not null) builder.Add(valueKeyword); builder.Add(period);
        return builder.ToImmutable();
    }
}
