using PayToCalc.Core.Paywall;

namespace PayToCalc.Core.Calculator;

/// <summary>Verbindet Rechenband, Verlauf und Demo-Paywall.</summary>
public sealed class CalculatorSession
{
    /// <summary>Höchstzahl der Einträge im Rechenband.</summary>
    public const int HistoryCapacity = 20;

    private readonly CalculatorEngine _engine;
    private readonly List<HistoryEntry> _history = [];

    /// <summary>Erzeugt eine Sitzung mit eigenem Rechenband.</summary>
    public CalculatorSession(PaywallService paywall)
        : this(new CalculatorEngine(), paywall)
    {
    }

    /// <summary>Erzeugt eine Sitzung mit übergebenem Rechenband.</summary>
    public CalculatorSession(CalculatorEngine engine, PaywallService paywall)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(paywall);
        _engine = engine;
        Paywall = paywall;
    }

    /// <summary>Das Rechenband.</summary>
    public CalculatorEngine Engine => _engine;

    /// <summary>Die Demo-Paywall.</summary>
    public PaywallService Paywall { get; }

    /// <summary>Der sichtbare Ausdruck.</summary>
    public string ExpressionText => _engine.ExpressionText;

    /// <summary>Der sichtbare Eintrag bzw. das Ergebnis.</summary>
    public string ResultText => _engine.ResultText;

    /// <summary>Wahr, wenn ein Fehler angezeigt wird.</summary>
    public bool HasError => _engine.HasError;

    /// <summary>Wahr, wenn das angezeigte Ergebnis verdeckt ist.</summary>
    public bool IsResultMasked { get; private set; }

    /// <summary>Der Verlauf, neueste Auswertung zuerst.</summary>
    public IReadOnlyList<HistoryEntry> History => _history;

    /// <summary>Verarbeitet eine Ziffer.</summary>
    public void InputDigit(char digit)
    {
        _engine.InputDigit(digit);
        IsResultMasked = false;
    }

    /// <summary>Verarbeitet Komma oder Punkt.</summary>
    public void InputDecimalSeparator()
    {
        _engine.InputDecimalSeparator();
        IsResultMasked = false;
    }

    /// <summary>Verarbeitet einen Operator.</summary>
    public bool InputOperator(char symbol)
    {
        var accepted = _engine.InputOperator(symbol);
        if (accepted)
        {
            IsResultMasked = false;
        }

        return accepted;
    }

    /// <summary>Wechselt das Vorzeichen.</summary>
    public void ToggleSign()
    {
        _engine.ToggleSign();
        IsResultMasked = false;
    }

    /// <summary>Teilt den Eintrag durch 100.</summary>
    public void Percent()
    {
        _engine.Percent();
        IsResultMasked = false;
    }

    /// <summary>Löscht die letzte Ziffer.</summary>
    public void Backspace()
    {
        _engine.Backspace();
        IsResultMasked = false;
    }

    /// <summary>Setzt den Eintrag auf 0.</summary>
    public void ClearEntry()
    {
        _engine.ClearEntry();
        IsResultMasked = false;
    }

    /// <summary>Löscht Eintrag, Ausdruck und Fehler.</summary>
    public void ClearAll()
    {
        _engine.ClearAll();
        IsResultMasked = false;
    }

    /// <summary>Wertet aus und zählt die Auswertung für die Demo-Paywall.</summary>
    public bool Evaluate()
    {
        if (!_engine.Evaluate())
        {
            return false;
        }

        IsResultMasked = !Paywall.RegisterSuccessfulEvaluation();
        AddHistory(new HistoryEntry(_engine.ExpressionText, _engine.ResultText, IsResultMasked));
        return true;
    }

    /// <summary>Schaltet die Demo frei.</summary>
    public void Unlock(PaywallPlan plan)
    {
        Paywall.Unlock(plan);
        IsResultMasked = false;
        RefreshHistoryMasks();
    }

    /// <summary>Setzt die Demo zurück.</summary>
    public void ResetDemo()
    {
        Paywall.Reset();
        IsResultMasked = false;
        RefreshHistoryMasks();
    }

    private void AddHistory(HistoryEntry entry)
    {
        _history.Insert(0, entry);
        while (_history.Count > HistoryCapacity)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private void RefreshHistoryMasks()
    {
        if (!Paywall.IsResultVisible)
        {
            return;
        }

        for (var i = 0; i < _history.Count; i++)
        {
            if (_history[i].IsMasked)
            {
                _history[i] = _history[i] with { IsMasked = false };
            }
        }
    }
}
