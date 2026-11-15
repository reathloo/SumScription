namespace SumScription.App.ViewModels;

/// <summary>Eine Zeile des Rechenbands in der Anzeige.</summary>
public sealed class HistoryRow
{
    /// <summary>Platzhalter für ein verdecktes Ergebnis.</summary>
    public const string MaskedResult = "• • • •";

    /// <summary>Erzeugt eine Zeile.</summary>
    public HistoryRow(string expression, string result, bool isMasked, double opacity)
    {
        Expression = expression;
        Result = result;
        IsMasked = isMasked;
        Opacity = opacity;
    }

    /// <summary>Der ausgewertete Ausdruck.</summary>
    public string Expression { get; }

    /// <summary>Das Ergebnis, unmaskiert.</summary>
    public string Result { get; }

    /// <summary>Das sichtbare Ergebnis.</summary>
    public string ResultText => IsMasked ? MaskedResult : Result;

    /// <summary>Wahr, wenn das Ergebnis verdeckt ist.</summary>
    public bool IsMasked { get; }

    /// <summary>Deckkraft der Zeile (ältere Zeilen sind schwächer).</summary>
    public double Opacity { get; }

    /// <summary>Sprechendname für Screenreader.</summary>
    public string AutomationName => $"{Expression} gleich {ResultText}";
}
