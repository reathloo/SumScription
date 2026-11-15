namespace PayToCalc.Core.Calculator;

/// <summary>Fehlerzustände der Auswertung.</summary>
public enum CalculatorError
{
    /// <summary>Kein Fehler.</summary>
    None,

    /// <summary>Division durch null.</summary>
    DivisionByZero,

    /// <summary>Wertebereich von <see cref="decimal"/> überschritten.</summary>
    Overflow,
}

/// <summary>Sichtbare deutsche Texte der Fehlerzustände.</summary>
public static class CalculatorErrorText
{
    /// <summary>Liefert den Anzeigetext zu einem Fehlerzustand.</summary>
    public static string ToDisplayText(this CalculatorError error) => error switch
    {
        CalculatorError.None => string.Empty,
        CalculatorError.DivisionByZero => "Division durch null",
        CalculatorError.Overflow => "Zahl zu groß",
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unbekannter Fehler."),
    };
}
