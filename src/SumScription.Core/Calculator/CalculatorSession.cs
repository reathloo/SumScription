using SumScription.Core.Paywall;

namespace SumScription.Core.Calculator;

/// <summary>Connects the calculator engine, history, and parody paywall.</summary>
public sealed class CalculatorSession
{
    /// <summary>Maximum number of history entries.</summary>
    public const int HistoryCapacity = 20;

    private readonly CalculatorEngine _engine;
    private readonly List<HistoryEntry> _history = [];

    /// <summary>Creates a session with a new calculator engine.</summary>
    public CalculatorSession(PaywallService paywall)
        : this(new CalculatorEngine(), paywall)
    {
    }

    /// <summary>Creates a session with the provided calculator engine.</summary>
    public CalculatorSession(CalculatorEngine engine, PaywallService paywall)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(paywall);
        _engine = engine;
        Paywall = paywall;
    }

    /// <summary>The calculator engine.</summary>
    public CalculatorEngine Engine => _engine;

    /// <summary>The parody paywall.</summary>
    public PaywallService Paywall { get; }

    /// <summary>The visible expression.</summary>
    public string ExpressionText => _engine.ExpressionText;

    /// <summary>The visible entry or result.</summary>
    public string DisplayResult => _engine.DisplayResult;

    /// <summary>Whether an error is visible.</summary>
    public bool HasError => _engine.HasError;

    /// <summary>The current calculator error.</summary>
    public CalculatorError Error => _engine.Error;

    /// <summary>Whether the displayed result is masked.</summary>
    public bool IsResultMasked { get; private set; }

    /// <summary>History with the newest evaluation first.</summary>
    public IReadOnlyList<HistoryEntry> History => _history;

    /// <summary>Handles a digit.</summary>
    public void InputDigit(char digit)
    {
        _engine.InputDigit(digit);
        IsResultMasked = false;
    }

    /// <summary>Handles a comma or period decimal separator.</summary>
    public void InputDecimalSeparator()
    {
        _engine.InputDecimalSeparator();
        IsResultMasked = false;
    }

    /// <summary>Handles an operator.</summary>
    public bool InputOperator(char symbol)
    {
        if (IsResultMasked)
        {
            return false;
        }

        var accepted = _engine.InputOperator(symbol);
        if (accepted)
        {
            IsResultMasked = false;
        }

        return accepted;
    }

    /// <summary>Toggles the sign.</summary>
    public void ToggleSign()
    {
        if (IsResultMasked)
        {
            return;
        }

        _engine.ToggleSign();
        IsResultMasked = false;
    }

    /// <summary>Uses one premium credit and divides the entry by one hundred.</summary>
    public bool TryPercent()
    {
        if (IsResultMasked || _engine.HasError)
        {
            return false;
        }

        if (!Paywall.TryConsumePremiumPercentCredit())
        {
            return false;
        }

        _engine.Percent();
        IsResultMasked = false;
        return true;
    }

    /// <summary>Deletes the last digit.</summary>
    public void Backspace()
    {
        if (IsResultMasked)
        {
            return;
        }

        _engine.Backspace();
        IsResultMasked = false;
    }

    /// <summary>Resets the current entry to zero.</summary>
    public void ClearEntry()
    {
        _engine.ClearEntry();
        IsResultMasked = false;
    }

    /// <summary>Clears the entry, expression, and error.</summary>
    public void ClearAll()
    {
        _engine.ClearAll();
        IsResultMasked = false;
    }

    /// <summary>Evaluates and registers the result with the parody paywall.</summary>
    public bool Evaluate()
    {
        if (!_engine.Evaluate())
        {
            return false;
        }

        IsResultMasked = !Paywall.RegisterSuccessfulEvaluation();
        AddHistory(new HistoryEntry(_engine.ExpressionText, _engine.DisplayResult, IsResultMasked));
        return true;
    }

    /// <summary>Fictionally unlocks result visibility.</summary>
    public void Unlock(PaywallPlan plan)
    {
        Paywall.Unlock(plan);
        IsResultMasked = false;

        if (plan == PaywallPlan.SingleCalculation)
        {
            if (_history.Count > 0 && _history[0].IsMasked)
            {
                _history[0] = _history[0] with { IsMasked = false };
            }

            return;
        }

        RefreshHistoryMasks();
    }

    /// <summary>Resets local parody state.</summary>
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
