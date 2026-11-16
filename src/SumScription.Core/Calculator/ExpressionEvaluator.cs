namespace SumScription.Core.Calculator;

/// <summary>Evaluates operand and operator sequences using standard precedence.</summary>
public static class ExpressionEvaluator
{
    /// <summary>
    /// Evaluates the sequence. <paramref name="operands"/> must contain exactly one more item
    /// than <paramref name="operators"/>.
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
            throw new ArgumentException("Every operator requires exactly one right-hand operand.", nameof(operators));
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
                        throw new ArgumentOutOfRangeException(nameof(operators), "Unknown operator.");
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
