using System.Globalization;

namespace SumScription.Core.Calculator;

/// <summary>Das eine Zahlenformat der App: deutsch, ohne Exponent, ohne Tausendertrenner.</summary>
public static class NumberFormatter
{
    /// <summary>Höchstzahl signifikanter Stellen für Eintrag und Ergebnis.</summary>
    public const int SignificantDigits = 15;

    /// <summary>Dezimaltrennzeichen der Anzeige.</summary>
    public const string Separator = ",";

    private const int MaxScale = 28;

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("de-DE");

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    /// <summary>Formatiert einen Wert mit höchstens 15 signifikanten Stellen.</summary>
    public static string Format(decimal value)
    {
        var rounded = RoundToSignificantDigits(value, SignificantDigits);
        if (rounded == 0m)
        {
            return "0";
        }

        return rounded.ToString("0.###############################", DisplayCulture);
    }

    /// <summary>Rundet auf die angegebene Zahl signifikanter Stellen.</summary>
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

    /// <summary>Parst einen Eintrag der Anzeige (Komma oder Punkt) oder liefert 0.</summary>
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
