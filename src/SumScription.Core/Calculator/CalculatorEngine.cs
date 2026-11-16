using System.Text;

namespace SumScription.Core.Calculator;

/// <summary>State machine for calculator entry, expression, result, and error.</summary>
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
    private bool _hasUserInput;

    private enum Phase
    {
        Typing,
        ShowingResult,
        Error,
    }

    /// <summary>The visible expression, for example <c>1 + 2 × 3</c>.</summary>
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

    /// <summary>The visible entry or most recent result.</summary>
    public string DisplayResult => _phase switch
    {
        Phase.ShowingResult => NumberFormatter.Format(_result),
        Phase.Error => _error.ToDisplayText(),
        _ => _entry,
    };

    /// <summary>The current error, or <see cref="CalculatorError.None"/>.</summary>
    public CalculatorError Error => _phase == Phase.Error ? _error : CalculatorError.None;

    /// <summary>Whether an error is visible.</summary>
    public bool HasError => _phase == Phase.Error;

    /// <summary>Whether the result line shows the most recent evaluation.</summary>
    public bool IsShowingResult => _phase == Phase.ShowingResult;

    /// <summary>Whether the entry is blank directly after an operator.</summary>
    public bool IsEntryBlank => _entryBlank;

    /// <summary>The current entry as a number.</summary>
    public decimal EntryValue => _phase == Phase.ShowingResult ? _result : NumberFormatter.ParseEntry(_entry);

    /// <summary>Handles one digit from zero through nine.</summary>
    public void InputDigit(char digit)
    {
        if (!char.IsAsciiDigit(digit))
        {
            throw new ArgumentOutOfRangeException(nameof(digit), digit, "Only digits 0–9 are supported.");
        }

        if (_phase != Phase.Typing)
        {
            StartNewExpression();
        }

        AppendDigit(digit);
    }

    /// <summary>Handles a comma or period; both create one display separator.</summary>
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

        _hasUserInput = true;

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

    /// <summary>Handles <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c>, and their display symbols.</summary>
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

        _hasUserInput = true;

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

    /// <summary>Toggles the sign of the current entry.</summary>
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
            _hasUserInput = true;
            return;
        }

        if (NumberFormatter.ParseEntry(_entry) == 0m)
        {
            return;
        }

        _hasUserInput = true;

        _entry = _entry.StartsWith('-')
            ? _entry[1..]
            : "-" + _entry;
    }

    /// <summary>Divides the current entry by one hundred.</summary>
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
        _hasUserInput |= value != 0m;
    }

    /// <summary>Deletes the last digit of the current entry.</summary>
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

    /// <summary>
    /// Resets the entry to zero while keeping the expression. After a result, it starts
    /// a new entry because the previous result completed the expression.
    /// </summary>
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

    /// <summary>Clears the entry, expression, and error state.</summary>
    public void ClearAll()
    {
        StartNewExpression();
        _result = 0m;
        _evaluatedExpression = string.Empty;
    }

    /// <summary>
    /// Evaluates the expression. Returns <c>false</c> on error, when a result is already
    /// visible, or when no input has been provided.
    /// </summary>
    public bool Evaluate()
    {
        if (_phase != Phase.Typing || !_hasUserInput)
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
        _hasUserInput = false;
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
        _hasUserInput = true;
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
