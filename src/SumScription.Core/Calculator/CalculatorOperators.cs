namespace SumScription.Core.Calculator;

/// <summary>The four supported calculator operators.</summary>
public enum CalculatorOperator
{
    /// <summary>Addition.</summary>
    Add,

    /// <summary>Subtraktion.</summary>
    Subtract,

    /// <summary>Multiplikation.</summary>
    Multiply,

    /// <summary>Division.</summary>
    Divide,
}

/// <summary>Operator display and parsing.</summary>
public static class CalculatorOperators
{
    /// <summary>Display minus sign (U+2212).</summary>
    public const char MinusSign = '\u2212';

    /// <summary>Display multiplication sign (U+00D7).</summary>
    public const char MultiplicationSign = '\u00D7';

    /// <summary>Display division sign (U+00F7).</summary>
    public const char DivisionSign = '\u00F7';

    /// <summary>Returns the display symbol for an operator.</summary>
    public static string ToSymbol(this CalculatorOperator op) => op switch
    {
        CalculatorOperator.Add => "+",
        CalculatorOperator.Subtract => MinusSign.ToString(),
        CalculatorOperator.Multiply => MultiplicationSign.ToString(),
        CalculatorOperator.Divide => DivisionSign.ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown operator."),
    };

    /// <summary>Parses display and keyboard symbols for an operator.</summary>
    public static bool TryParse(char symbol, out CalculatorOperator op)
    {
        switch (symbol)
        {
            case '+':
                op = CalculatorOperator.Add;
                return true;
            case '-' or MinusSign:
                op = CalculatorOperator.Subtract;
                return true;
            case '*' or MultiplicationSign:
                op = CalculatorOperator.Multiply;
                return true;
            case '/' or DivisionSign:
                op = CalculatorOperator.Divide;
                return true;
            default:
                op = CalculatorOperator.Add;
                return false;
        }
    }
}
