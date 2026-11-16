namespace SumScription.App.ViewModels;

/// <summary>One calculator history row in the UI.</summary>
public sealed class HistoryRow
{
    /// <summary>Placeholder for a masked result.</summary>
    public const string MaskedResult = "• • • •";

    /// <summary>Creates a history row.</summary>
    public HistoryRow(string expression, string result, bool isMasked, double opacity)
    {
        Expression = expression;
        Result = result;
        IsMasked = isMasked;
        Opacity = opacity;
    }

    /// <summary>The evaluated expression.</summary>
    public string Expression { get; }

    /// <summary>The unmasked result.</summary>
    public string Result { get; }

    /// <summary>The visible result.</summary>
    public string ResultText => IsMasked ? MaskedResult : Result;

    /// <summary>Whether the result is masked.</summary>
    public bool IsMasked { get; }

    /// <summary>Row opacity; older rows are dimmer.</summary>
    public double Opacity { get; }

    /// <summary>Accessible name for screen readers.</summary>
    public string AutomationName => $"{Expression} equals {ResultText}";
}
