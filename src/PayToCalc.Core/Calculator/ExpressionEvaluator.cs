namespace PayToCalc.Core.Calculator;

/// <summary>Wertet eine Operanden-/Operatorfolge mit Punkt-vor-Strich aus.</summary>
public static class ExpressionEvaluator
{
    /// <summary>
    /// Wertet die Folge aus. <paramref name="operands"/> muss genau ein Element mehr
    /// enthalten als <paramref name="operators"/>.
    /// </summary>
    public static bool TryEvaluate(
        IReadOnlyList<decimal> operands,
        IReadOnlyList<CalculatorOperator> operators,
        out decimal result,
        out CalculatorError error)
    {
        ArgumentNullException.ThrowIfNull(operands);
        ArgumentNullException.ThrowIfNull(operators);

        result = 0m;
        error = CalculatorError.None;

        if (operands.Count == 0)
        {
            return true;
        }

        if (operands.Count != operators.Count + 1)
        {
            throw new ArgumentException("Zu jedem Operator gehört genau eine rechte Zahl.", nameof(operators));
        }

        try
        {
            var sum = 0m;
            var term = operands[0];

            for (var i = 0; i < operators.Count; i++)
            {
                var next = operands[i + 1];
                switch (operators[i])
                {
                    case CalculatorOperator.Add:
                        sum += term;
                        term = next;
                        break;
                    case CalculatorOperator.Subtract:
                        sum += term;
                        term = -next;
                        break;
                    case CalculatorOperator.Multiply:
                        term = checked(term * next);
                        break;
                    case CalculatorOperator.Divide:
                        if (next == 0m)
                        {
                            error = CalculatorError.DivisionByZero;
                            return false;
                        }

                        term /= next;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operators), "Unbekannter Operator.");
                }
            }

            sum += term;
            result = sum;
            return true;
        }
        catch (OverflowException)
        {
            result = 0m;
            error = CalculatorError.Overflow;
            return false;
        }
    }
}
