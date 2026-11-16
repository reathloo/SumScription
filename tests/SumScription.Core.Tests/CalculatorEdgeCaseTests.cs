using SumScription.Core.Calculator;

namespace SumScription.Core.Tests;

public sealed class CalculatorEdgeCaseTests
{
    [Theory]
    [InlineData("10-6/3", "10 − 6 ÷ 3", "8")]
    [InlineData("8/2-3*2", "8 ÷ 2 − 3 × 2", "-2")]
    [InlineData("1+2*3-4", "1 + 2 × 3 − 4", "3")]
    public void Precedence_AlsoAppliesToDivisionAndSubtraction(string input, string expression, string result)
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, input);

        Assert.Equal(expression, engine.ExpressionText);

        Assert.True(engine.Evaluate());

        Assert.Equal(result, engine.DisplayResult);
        Assert.Equal(expression, engine.ExpressionText);
    }

    [Theory]
    [InlineData("3-10+4", "3 − 10 + 4", "-3")]
    [InlineData("2-10/2", "2 − 10 ÷ 2", "-3")]
    [InlineData("5-8", "5 − 8", "-3")]
    public void NegativeIntermediateResults_AreCarriedToTheEnd(string input, string expression, string result)
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, input);

        Assert.True(engine.Evaluate());
        Assert.Equal(result, engine.DisplayResult);
        Assert.Equal(expression, engine.ExpressionText);
    }

    [Fact]
    public void TrailingOperator_IsDroppedFromTheEvaluatedExpression()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1+2+3*");

        Assert.Equal("1 + 2 + 3 ×", engine.ExpressionText);

        Assert.True(engine.Evaluate());

        Assert.Equal("1 + 2 + 3", engine.ExpressionText);
        Assert.Equal("6", engine.DisplayResult);
    }

    [Fact]
    public void TrailingOperator_OnSingleValue_KeepsThatValue()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "7*");

        Assert.Equal("7 ×", engine.ExpressionText);

        Assert.True(engine.Evaluate());

        Assert.Equal("7", engine.ExpressionText);
        Assert.Equal("7", engine.DisplayResult);
    }

    [Theory]
    [InlineData("2/3", "0,666666666666667")]
    [InlineData("1/7", "0,142857142857143")]
    public void Result_IsRoundedToFifteenSignificantDigits(string input, string expected)
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, input);

        Assert.True(engine.Evaluate());
        Assert.Equal(expected, engine.DisplayResult);
        Assert.Equal(NumberFormatter.SignificantDigits, engine.DisplayResult.Split(NumberFormatter.Separator)[1].Length);
    }

    [Fact]
    public void Result_WithoutFraction_IsRoundedToFifteenSignificantDigits()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "999999999999999*9");

        Assert.True(engine.Evaluate());

        Assert.Equal("8999999999999990", engine.DisplayResult);
        Assert.DoesNotContain(NumberFormatter.Separator, engine.DisplayResult, StringComparison.Ordinal);
        Assert.DoesNotContain(".", engine.DisplayResult, StringComparison.Ordinal);
        Assert.DoesNotContain("E", engine.DisplayResult, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Input_StopsAtFifteenSignificantDigits_IgnoringLeadingZeros()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "0000001234567890123456");

        Assert.Equal("123456789012345", engine.DisplayResult);
        Assert.Equal(NumberFormatter.SignificantDigits, engine.DisplayResult.Length);
    }

    [Fact]
    public void Input_StopsAtFifteenSignificantDigits_InDecimalEntry()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "0,1234567890123456789");

        Assert.Equal("0,123456789012345", engine.DisplayResult);
    }

    [Fact]
    public void Input_AcceptsLeadingSeparator_AndShowsZeroBeforeIt()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, ",5+1");

        Assert.Equal("0,5 + 1", engine.ExpressionText);

        Assert.True(engine.Evaluate());

        Assert.Equal("1,5", engine.DisplayResult);
    }

    [Fact]
    public void Percent_DividesNegativeEntryByHundred_AndIsRepeatable()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "250");
        engine.ToggleSign();
        engine.Percent();

        Assert.Equal("-2,5", engine.DisplayResult);

        engine.Percent();

        Assert.Equal("-0,025", engine.DisplayResult);

        Input.Enter(engine, "5");

        Assert.Equal("-0,0255", engine.DisplayResult);
    }

    [Fact]
    public void Percent_AfterResult_StartsNewEntryFromResult()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "50*2");
        Assert.True(engine.Evaluate());

        engine.Percent();

        Assert.Equal("1", engine.DisplayResult);
        Assert.Equal("1", engine.ExpressionText);
        Assert.False(engine.IsShowingResult);

        Assert.True(engine.Evaluate());
        Assert.Equal("1", engine.DisplayResult);
    }

    [Fact]
    public void ToggleSign_OnZero_IsIgnored()
    {
        var engine = new CalculatorEngine();

        engine.ToggleSign();
        engine.InputDigit('7');

        Assert.Equal("7", engine.DisplayResult);
        Assert.Equal("7", engine.ExpressionText);
    }

    [Fact]
    public void ToggleSign_OnResult_StartsNewEntryWithNegatedValue()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1/7");
        Assert.True(engine.Evaluate());

        engine.ToggleSign();

        Assert.Equal("-0,142857142857143", engine.DisplayResult);
        Assert.Equal("-0,142857142857143", engine.ExpressionText);
        Assert.False(engine.IsShowingResult);
    }

    [Fact]
    public void ToggleSign_OnBlankEntryAfterOperator_KeepsEntryBlank()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "12+");
        engine.ToggleSign();

        Assert.True(engine.IsEntryBlank);
        Assert.Equal("12 +", engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);

        engine.InputDigit('7');

        Assert.Equal("12 + 7", engine.ExpressionText);
        Assert.False(engine.IsEntryBlank);
    }

    [Fact]
    public void Backspace_OnBlankEntryAfterOperator_ChangesNothing()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "12+");
        engine.Backspace();

        Assert.True(engine.IsEntryBlank);
        Assert.Equal("12 +", engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);
    }

    [Fact]
    public void Backspace_OnNegativeSingleDigit_LeavesZero()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "5");
        engine.ToggleSign();
        Assert.Equal("-5", engine.DisplayResult);

        engine.Backspace();

        Assert.Equal("0", engine.DisplayResult);
    }

    [Fact]
    public void ClearEntryKeepsExpression_WhileClearAllRemovesBoth()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "12+34");
        engine.ClearEntry();

        Assert.Equal("12 + 0", engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);

        engine.ClearAll();

        Assert.Equal(string.Empty, engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);
        Assert.False(engine.IsEntryBlank);
    }

    [Fact]
    public void ClearEntry_DuringError_IsIgnored_AndClearAllRecovers()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1/0");
        Assert.False(engine.Evaluate());

        engine.ClearEntry();

        Assert.True(engine.HasError);
        Assert.Equal(CalculatorError.DivisionByZero, engine.Error);
        Assert.Equal("Cannot divide by zero", engine.DisplayResult);
        Assert.Equal("1 ÷ 0", engine.ExpressionText);

        engine.ClearAll();

        Assert.False(engine.HasError);
        Assert.Equal(CalculatorError.None, engine.Error);
        Assert.Equal(string.Empty, engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);
    }

    [Fact]
    public void Expression_AfterEquals_RemainsReadable_AndIsNotEvaluatedTwice()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1+2*3");

        Assert.True(engine.Evaluate());

        Assert.Equal("1 + 2 × 3", engine.ExpressionText);
        Assert.Equal("7", engine.DisplayResult);
        Assert.True(engine.IsShowingResult);
        Assert.False(engine.Evaluate());
        Assert.Equal("1 + 2 × 3", engine.ExpressionText);
    }
}
