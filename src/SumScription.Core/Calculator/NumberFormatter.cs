using System.Globalization;

namespace SumScription.Core.Calculator;

/// <summary>The app's number format: German decimal separator, no exponent or grouping.</summary>
public static class NumberFormatter
{
    /// <summary>Maximum significant digits for entries and results.</summary>
    public const int SignificantDigits = 15;

    /// <summary>Display decimal separator.</summary>
    public const string Separator = ",";

    private const int MaxScale = 28;

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("de-DE");

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    /// <summary>Formats a value with at most fifteen significant digits.</summary>
    public static string Format(decimal value)
    {
        var rounded = RoundToSignificantDigits(value, SignificantDigits);
        if (rounded == 0m)
        {
            return "0";
        }

        return rounded.ToString("0.###############################", DisplayCulture);
    }

    /// <summary>Rounds to the requested number of significant digits.</summary>
    public static decimal RoundToSignificantDigits(decimal value, int digits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digits, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digits, MaxScale);

        if (value == 0m)
        {
            return 0m;
        }

        var integerDigits = CountIntegerDigits(decimal.Truncate(value));

        if (integerDigits > digits)
        {
            return RoundIntegerDigits(value, integerDigits - digits);
        }

        int decimals;
        if (integerDigits == digits)
        {
            decimals = 0;
        }
        else
        {
            var leadingZeros = 0;
            if (integerDigits == 0)
            {
                var plain = UnsignedText(value, "F28");
                var fractionStart = plain.IndexOf('.', StringComparison.Ordinal) + 1;
                while (fractionStart + leadingZeros < plain.Length && plain[fractionStart + leadingZeros] == '0')
                {
                    leadingZeros++;
                }
            }

            decimals = Math.Min(MaxScale, digits - integerDigits + leadingZeros);
        }

        return decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    private static int CountIntegerDigits(decimal value)
    {
        if (value == 0m)
        {
            return 0;
        }

        var text = UnsignedText(value, "F0");
        return text.Length;
    }

    private static string UnsignedText(decimal value, string format)
    {
        var text = value.ToString(format, InvariantCulture);
        return text.StartsWith('-') ? text[1..] : text;
    }

    private static decimal RoundIntegerDigits(decimal value, int dropDigits)
    {
        var factor = PowerOfTen(dropDigits);
        var scaled = decimal.Round(value / factor, 0, MidpointRounding.AwayFromZero);

        if (scaled == 0m)
        {
            return 0m;
        }

        try
        {
            return scaled * factor;
        }
        catch (OverflowException)
        {
            return (scaled - Math.Sign(scaled)) * factor;
        }
    }

    private static decimal PowerOfTen(int exponent)
    {
        var factor = 1m;
        for (var i = 0; i < exponent; i++)
        {
            factor *= 10m;
        }

        return factor;
    }

    /// <summary>Parses a display entry containing a comma or period, or returns zero.</summary>
    public static decimal ParseEntry(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return 0m;
        }

        var normalized = entry.Replace('.', ',');
        if (normalized.EndsWith(','))
        {
            normalized = normalized[..^1];
        }

        if (normalized is "" or "-")
        {
            return 0m;
        }

        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, DisplayCulture, out var value)
            ? value
            : 0m;
    }
}
