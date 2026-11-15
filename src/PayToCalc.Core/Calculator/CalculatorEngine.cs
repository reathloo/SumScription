using System.Text;

namespace PayToCalc.Core.Calculator;

/// <summary>Zustandsmaschine des Rechenbands: Eintrag, Ausdruck, Fehler.</summary>
public sealed class CalculatorEngine
{
    private const string ZeroEntry = "0";

    private readonly List<decimal> _operands = [];
    private readonly List<CalculatorOperator> _operators = [];

    private string _entry = ZeroEntry;
    private bool _entryBlank;
    private Phase _phase = Phase.Typing;
    private CalculatorError _error = CalculatorError.None;
    private decimal _result;
    private string _evaluatedExpression = string.Empty;

    private enum Phase
    {
        Typing,
        ShowingResult,
        Error,
    }

    /// <summary>Der sichtbare Ausdruck, z. B. <c>1 + 2 × 3</c>.</summary>
    public string ExpressionText
    {
        get
        {
            if (_phase == Phase.ShowingResult)
            {
                return _evaluatedExpression;
            }

            if (_operands.Count == 0 && !_entryBlank && _entry == ZeroEntry)
            {
                return string.Empty;
            }

            return RenderExpression(_operands, _operators, _entryBlank ? null : _entry);
        }
    }

    /// <summary>Der sichtbare Eintrag bzw. das Ergebnis der letzten Auswertung.</summary>
    public string DisplayResult => _phase switch
    {
        Phase.ShowingResult => NumberFormatter.Format(_result),
        Phase.Error => _error.ToDisplayText(),
        _ => _entry,
    };

    /// <summary>Der Fehlerzustand, sonst <see cref="CalculatorError.None"/>.</summary>
    public CalculatorError Error => _phase == Phase.Error ? _error : CalculatorError.None;

    /// <summary>Wahr, wenn ein Fehler angezeigt wird.</summary>
    public bool HasError => _phase == Phase.Error;

    /// <summary>Wahr, wenn die Ergebniszeile das letzte Ergebnis zeigt.</summary>
    public bool IsShowingResult => _phase == Phase.ShowingResult;

    /// <summary>Wahr, wenn der Eintrag leer ist (direkt nach einem Operator).</summary>
    public bool IsEntryBlank => _entryBlank;

    /// <summary>Der aktuelle Eintrag als Zahl.</summary>
    public decimal EntryValue => _phase == Phase.ShowingResult ? _result : NumberFormatter.ParseEntry(_entry);

    /// <summary>Verarbeitet eine Ziffer 0–9.</summary>
    public void InputDigit(char digit)
    {
        if (!char.IsAsciiDigit(digit))
        {
            throw new ArgumentOutOfRangeException(nameof(digit), digit, "Nur Ziffern 0–9.");
        }

        if (_phase != Phase.Typing)
        {
            StartNewExpression();
        }

        AppendDigit(digit);
    }

    /// <summary>Verarbeitet Komma oder Punkt; beide erzeugen genau ein Komma.</summary>
    public void InputDecimalSeparator()
    {
        if (_phase == Phase.Error)
        {
            return;
        }

        if (_phase == Phase.ShowingResult)
        {
            StartNewExpression();
        }

        if (_entryBlank)
        {
            _entry = ZeroEntry + NumberFormatter.Separator;
            _entryBlank = false;
            return;
        }

        if (!_entry.Contains(NumberFormatter.Separator, StringComparison.Ordinal))
        {
            _entry += NumberFormatter.Separator;
        }
    }

    /// <summary>Verarbeitet <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c> und deren Anzeigezeichen.</summary>
    public bool InputOperator(char symbol)
    {
        if (!CalculatorOperators.TryParse(symbol, out var op))
        {
            return false;
        }

        if (_phase == Phase.Error)
        {
            return false;
        }

        if (_phase == Phase.ShowingResult)
        {
            ContinueWithResult();
        }

        if (_entryBlank && _operators.Count > 0)
        {
            _operators[^1] = op;
            return true;
        }

        _operands.Add(NumberFormatter.ParseEntry(_entry));
        _operators.Add(op);
        _entry = ZeroEntry;
        _entryBlank = true;
        return true;
    }

    /// <summary>Wechselt das Vorzeichen des Eintrags.</summary>
    public void ToggleSign()
    {
        if (_phase == Phase.Error)
        {
            return;
        }

        if (_phase == Phase.ShowingResult)
        {
            var value = _result;
            StartNewExpression();
            _entry = NumberFormatter.Format(-value);
            _entryBlank = false;
            return;
        }

        if (NumberFormatter.ParseEntry(_entry) == 0m)
        {
            return;
        }

        _entry = _entry.StartsWith('-')
            ? _entry[1..]
            : "-" + _entry;
    }

    /// <summary>Teilt den Eintrag durch 100.</summary>
    public void Percent()
    {
        if (_phase == Phase.Error)
        {
            return;
        }

        var value = EntryValue;
        if (_phase == Phase.ShowingResult)
        {
            StartNewExpression();
        }

        _entry = NumberFormatter.Format(value / 100m);
        _entryBlank = false;
        _phase = Phase.Typing;
    }

    /// <summary>Löscht die letzte Ziffer des Eintrags.</summary>
    public void Backspace()
    {
        if (_phase != Phase.Typing || _entryBlank)
        {
            return;
        }

        if (_entry.Length <= 1)
        {
            _entry = ZeroEntry;
            return;
        }

        _entry = _entry[..^1];
        if (_entry is "" or "-")
        {
            _entry = ZeroEntry;
        }
    }

    /// <summary>Setzt den Eintrag auf 0, der Ausdruck bleibt stehen.</summary>
    public void ClearEntry()
    {
        if (_phase == Phase.Error)
        {
            return;
        }

        if (_phase == Phase.ShowingResult)
        {
            StartNewExpression();
            return;
        }

        _entry = ZeroEntry;
        _entryBlank = false;
    }

    /// <summary>Löscht Eintrag, Ausdruck und Fehlerzustand.</summary>
    public void ClearAll()
    {
        StartNewExpression();
        _result = 0m;
        _evaluatedExpression = string.Empty;
    }

    /// <summary>
    /// Wertet den Ausdruck aus. Liefert <c>false</c> bei Fehler oder wenn bereits
    /// ein Ergebnis angezeigt wird.
    /// </summary>
    public bool Evaluate()
    {
        if (_phase != Phase.Typing)
        {
            return false;
        }

        var operands = new List<decimal>(_operands);
        var operators = new List<CalculatorOperator>(_operators);

        if (_entryBlank && operators.Count > 0)
        {
            operators.RemoveAt(operators.Count - 1);
        }
        else
        {
            operands.Add(NumberFormatter.ParseEntry(_entry));
        }

        var expression = RenderExpression(operands, operators, null);

        if (!ExpressionEvaluator.TryEvaluate(operands, operators, out var value, out var error))
        {
            _error = error;
            _phase = Phase.Error;
            return false;
        }

        _result = value;
        _phase = Phase.ShowingResult;
        _evaluatedExpression = expression;
        _operands.Clear();
        _operands.Add(value);
        _operators.Clear();
        _entry = NumberFormatter.Format(value);
        _entryBlank = false;
        return true;
    }

    private static string RenderExpression(
        IReadOnlyList<decimal> operands,
        IReadOnlyList<CalculatorOperator> operators,
        string? entry)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < operands.Count; i++)
        {
            builder.Append(NumberFormatter.Format(operands[i]));
            if (i < operators.Count)
            {
                builder.Append(' ');
                builder.Append(operators[i].ToSymbol());
                builder.Append(' ');
            }
        }

        if (entry is not null)
        {
            builder.Append(entry);
        }

        return builder.ToString().TrimEnd();
    }

    private void StartNewExpression()
    {
        _operands.Clear();
        _operators.Clear();
        _entry = ZeroEntry;
        _entryBlank = false;
        _phase = Phase.Typing;
        _error = CalculatorError.None;
        _evaluatedExpression = string.Empty;
    }

    private void ContinueWithResult()
    {
        var value = _result;
        StartNewExpression();
        _entry = NumberFormatter.Format(value);
    }

    private void AppendDigit(char digit)
    {
        if (CountSignificantDigits(_entry) >= NumberFormatter.SignificantDigits)
        {
            return;
        }

        if (_entryBlank || _entry == ZeroEntry)
        {
            _entry = digit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            _entry += digit;
        }

        _entryBlank = false;
        _phase = Phase.Typing;
    }

    private static int CountSignificantDigits(string entry)
    {
        var count = 0;
        var significant = false;
        foreach (var character in entry)
        {
            if (!char.IsAsciiDigit(character))
            {
                continue;
            }

            if (!significant && character == '0')
            {
                continue;
            }

            significant = true;
            count++;
        }

        return count;
    }
}
