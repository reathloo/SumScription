using SumScription.Core.Calculator;

namespace SumScription.Core.Tests;

public sealed class NumberFormatterTests
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("7", "7")]
    [InlineData("0,5", "0,5")]
    [InlineData("1234,56", "1234,56")]
    [InlineData("-3", "-3")]
    public void Format_ProducesPlainGermanText(string value, string expected)
    {
        var number = decimal.Parse(value, System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal(expected, NumberFormatter.Format(number));
    }

    [Fact]
    public void Format_RoundsToFifteenSignificantDigits()
    {
        var value = decimal.Parse("0,1234567890123456789", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal("0,123456789012346", NumberFormatter.Format(value));
    }

    [Fact]
    public void Format_TrimsTrailingZeros()
    {
        Assert.Equal("7", NumberFormatter.Format(7.00000000000000m));
        Assert.Equal("7,5", NumberFormatter.Format(7.500m));
    }

    [Fact]
    public void Format_RoundsIntegersToFifteenSignificantDigits()
    {
        Assert.Equal("1234567890123460", NumberFormatter.Format(1234567890123456m));
    }

    [Fact]
    public void Format_HandlesDecimalMaximumWithoutOverflow()
    {
        var text = NumberFormatter.Format(decimal.MaxValue);

        Assert.Equal("79228162514264300000000000000", text);
    }

    [Fact]
    public void Format_HandlesDecimalMinimumWithoutOverflow()
    {
        var text = NumberFormatter.Format(decimal.MinValue);

        Assert.Equal("-79228162514264300000000000000", text);
    }

    [Fact]
    public void Format_KeepsLargeValuesWithoutExponent()
    {
        Assert.Equal("123456789012345000", NumberFormatter.Format(123456789012345000m));
    }
}
