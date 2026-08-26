using Xunit;

namespace ReverseDSC.Tests;

public class DscStringEscaperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ReturnsEmptyForNullOrEmptyInput(string? input)
    {
        Assert.Equal(string.Empty, DscStringEscaper.Escape(input, false));
    }

    [Fact]
    public void EscapesBackticksByDoublingThem()
    {
        Assert.Equal("Hello``World", DscStringEscaper.Escape("Hello`World", false));
    }

    [Fact]
    public void EscapesDollarSignsByDefault()
    {
        Assert.Equal("Price is `$100", DscStringEscaper.Escape("Price is $100", false));
    }

    [Fact]
    public void PreservesDollarSignsWhenVariablesAreAllowed()
    {
        Assert.Equal("Value is $var", DscStringEscaper.Escape("Value is $var", true));
    }

    [Theory]
    [InlineData('„')]
    [InlineData('“')]
    [InlineData('”')]
    public void EscapesEuropeanQuotationMarks(char quotationMark)
    {
        Assert.Equal($"test`{quotationMark}value", DscStringEscaper.Escape($"test{quotationMark}value", false));
    }

    [Fact]
    public void EscapesDoubleQuotes()
    {
        Assert.Equal("She said `\"hello`\"", DscStringEscaper.Escape("She said \"hello\"", false));
    }

    [Fact]
    public void EscapesDoubleQuotesAndEscapeCharacters()
    {
        Assert.Equal(
            "She said `\"hello`\" with ```\"Escaped Text```\"",
            DscStringEscaper.Escape("She said \"hello\" with `\"Escaped Text`\"", false));
    }

    [Fact]
    public void ReturnsPlainTextUnchanged()
    {
        Assert.Equal("Normal text", DscStringEscaper.Escape("Normal text", false));
    }

    [Theory]
    [InlineData("Plain text")]
    [InlineData("She said \"hello\"")]
    [InlineData("Price is $100")]
    [InlineData("Path with ` backtick")]
    [InlineData("German „quotes“")]
    public void EscapedStringEvaluatesBackToTheOriginalValue(string original)
    {
        string escaped = DscStringEscaper.Escape(original, false);
        Assert.Equal(original, PowerShellExpression.Evaluate($"\"{escaped}\""));
    }
}
