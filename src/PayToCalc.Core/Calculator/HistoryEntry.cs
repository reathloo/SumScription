namespace PayToCalc.Core.Calculator;

/// <summary>Eine Zeile des Rechenbands.</summary>
/// <param name="Expression">Der ausgewertete Ausdruck.</param>
/// <param name="Value">Das Ergebnis als Anzeigetext.</param>
/// <param name="IsMasked">Wahr, wenn das Ergebnis von der Demo-Paywall verdeckt ist.</param>
public sealed record HistoryEntry(string Expression, string Value, bool IsMasked);
