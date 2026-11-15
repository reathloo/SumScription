namespace SumScription.Core.Calculator;

/// <summary>Die vier im Rechenband erlaubten Verknüpfungen.</summary>
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

/// <summary>Darstellung und Erkennung der Operatoren.</summary>
public static class CalculatorOperators
{
    /// <summary>Zeichen für das Minus im Ausdruck (U+2212).</summary>
    public const char MinusSign = '\u2212';

    /// <summary>Zeichen für die Multiplikation im Ausdruck (U+00D7).</summary>
    public const char MultiplicationSign = '\u00D7';

    /// <summary>Zeichen für die Division im Ausdruck (U+00F7).</summary>
    public const char DivisionSign = '\u00F7';

    /// <summary>Liefert das Anzeigezeichen des Operators.</summary>
    public static string ToSymbol(this CalculatorOperator op) => op switch
    {
        CalculatorOperator.Add => "+",
        CalculatorOperator.Subtract => MinusSign.ToString(),
        CalculatorOperator.Multiply => MultiplicationSign.ToString(),
        CalculatorOperator.Divide => DivisionSign.ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unbekannter Operator."),
    };

    /// <summary>Erkennt Anzeige- und Tastaturzeichen eines Operators.</summary>
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
