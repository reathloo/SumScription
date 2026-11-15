using SumScription.Core.Calculator;

namespace SumScription.Core.Tests;

public sealed class CalculatorEngineTests
{
    [Fact]
    public void PointBeforeLine_EvaluatesWholeExpression()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1+2*3");

        Assert.Equal("1 + 2 × 3", engine.ExpressionText);
        Assert.True(engine.Evaluate());
        Assert.Equal("7", engine.DisplayResult);
        Assert.Equal("1 + 2 × 3", engine.ExpressionText);
    }

    [Fact]
    public void TrailingOperator_IsIgnored()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1+2+");

        Assert.True(engine.Evaluate());
        Assert.Equal("3", engine.DisplayResult);
        Assert.Equal("1 + 2", engine.ExpressionText);
    }

    [Fact]
    public void LargeIntegerResult_IsRoundedToFifteenDigits()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "123456789012345*11");

        Assert.True(engine.Evaluate());
        Assert.Equal("1358024679135800", engine.DisplayResult);
    }

    [Theory]
    [InlineData("12+30", "42")]
    [InlineData("10-4*2", "2")]
    [InlineData("100/4/5", "5")]
    [InlineData("2+3*4-6/2", "11")]
    public void Expressions_FollowPrecedence(string input, string expected)
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, input);

        Assert.True(engine.Evaluate());
        Assert.Equal(expected, engine.DisplayResult);
    }

    [Fact]
    public void DivideByZero_ShowsMessage_AndLeavesErrorByDigit()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1/0");

        Assert.False(engine.Evaluate());
        Assert.True(engine.HasError);
        Assert.Equal("Division durch null", engine.DisplayResult);
        Assert.Equal("1 ÷ 0", engine.ExpressionText);

        engine.InputDigit('5');

        Assert.False(engine.HasError);
        Assert.Equal("5", engine.DisplayResult);
    }

    [Fact]
    public void DivideByZero_LeavesErrorByClearAll()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1/0");
        engine.Evaluate();
        engine.ClearAll();

        Assert.False(engine.HasError);
        Assert.Equal("0", engine.DisplayResult);
        Assert.Equal(string.Empty, engine.ExpressionText);
    }

    [Fact]
    public void Evaluate_WithoutAnyInput_ReturnsFalse()
    {
        var engine = new CalculatorEngine();

        Assert.False(engine.Evaluate());
        Assert.Equal(string.Empty, engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);
        Assert.False(engine.HasError);
    }

    [Fact]
    public void Evaluate_AfterClearAllWithoutInput_ReturnsFalse()
    {
        var engine = new CalculatorEngine();
        Input.Enter(engine, "1+2");
        engine.ClearAll();

        Assert.False(engine.Evaluate());
    }

    [Fact]
    public void Evaluate_AfterResultAndClearEntry_ReturnsFalse()
    {
        var engine = new CalculatorEngine();
        Input.Enter(engine, "1+2");
        Assert.True(engine.Evaluate());

        engine.ClearEntry();

        Assert.False(engine.Evaluate());
    }

    [Fact]
    public void Overflow_ShowsMessage()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "999999999999999*999999999999999*999999999999999");

        Assert.False(engine.Evaluate());
        Assert.True(engine.HasError);
        Assert.Equal("Zahl zu groß", engine.DisplayResult);
    }

    [Fact]
    public void OperatorDuringErrorIsIgnored()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1/0");
        engine.Evaluate();
        engine.InputOperator('+');

        Assert.True(engine.HasError);
        Assert.Equal("Division durch null", engine.DisplayResult);
    }

    [Fact]
    public void SignificantDigits_IgnoreFurtherDigits()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1234567890123456789");

        Assert.Equal("123456789012345", engine.DisplayResult);

        Assert.True(engine.Evaluate());
        Assert.Equal("123456789012345", engine.DisplayResult);
    }

    [Fact]
    public void FifteenSignificantDigits_AreFormattedWithoutTrailingZeros()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1/3");

        Assert.True(engine.Evaluate());
        Assert.Equal("0,333333333333333", engine.DisplayResult);
    }

    [Fact]
    public void Percent_DividesEntryByHundred()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "50");
        engine.Percent();

        Assert.Equal("0,5", engine.DisplayResult);
    }

    [Fact]
    public void ToggleSign_NegatesEntry()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "42");
        engine.ToggleSign();

        Assert.Equal("-42", engine.DisplayResult);

        engine.ToggleSign();

        Assert.Equal("42", engine.DisplayResult);
    }

    [Fact]
    public void Backspace_RemovesLastDigitAndKeepsZero()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "123");
        engine.Backspace();

        Assert.Equal("12", engine.DisplayResult);

        engine.Backspace();
        engine.Backspace();

        Assert.Equal("0", engine.DisplayResult);

        engine.Backspace();

        Assert.Equal("0", engine.DisplayResult);
    }

    [Fact]
    public void ClearEntry_KeepsExpression()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "12+34");
        engine.ClearEntry();

        Assert.Equal("12 + 0", engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);
    }

    [Fact]
    public void ClearAll_ResetsExpressionAndError()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "12+34");
        engine.ClearAll();

        Assert.Equal(string.Empty, engine.ExpressionText);
        Assert.Equal("0", engine.DisplayResult);
    }

    [Theory]
    [InlineData("1,5+2.5", "4")]
    [InlineData("0.5*4", "2")]
    [InlineData("7,25-0,25", "7")]
    public void DecimalInput_AcceptsCommaAndDot(string input, string expected)
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, input);

        Assert.True(engine.Evaluate());
        Assert.Equal(expected, engine.DisplayResult);
    }

    [Fact]
    public void DecimalSeparator_IsShownExactlyOnce()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1,,.5");

        Assert.Equal("1,5", engine.DisplayResult);
    }

    [Fact]
    public void DigitAfterEquals_StartsNewExpression()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1+2");
        engine.Evaluate();
        engine.InputDigit('9');

        Assert.Equal("9", engine.ExpressionText);
        Assert.Equal("9", engine.DisplayResult);
    }

    [Fact]
    public void OperatorAfterEquals_ContinuesWithResult()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1+2");
        engine.Evaluate();
        engine.InputOperator('*');
        engine.InputDigit('4');

        Assert.Equal("3 × 4", engine.ExpressionText);

        Assert.True(engine.Evaluate());
        Assert.Equal("12", engine.DisplayResult);
    }

    [Fact]
    public void RepeatedEquals_DoesNotEvaluateAgain()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "1+2");
        Assert.True(engine.Evaluate());
        Assert.False(engine.Evaluate());
    }

    [Fact]
    public void OperatorAfterOperator_ReplacesOperator()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "8+*2");

        Assert.Equal("8 × 2", engine.ExpressionText);
    }

    [Fact]
    public void NegativeResult_IsFormatted()
    {
        var engine = new CalculatorEngine();

        Input.Enter(engine, "2-5");

        Assert.True(engine.Evaluate());
        Assert.Equal("-3", engine.DisplayResult);
    }
}
