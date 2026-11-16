namespace SumScription.Core.Calculator;

/// <summary>Calculator evaluation errors.</summary>
public enum CalculatorError
{
    /// <summary>No error.</summary>
    None,

    /// <summary>Division by zero.</summary>
    DivisionByZero,

    /// <summary>The <see cref="decimal"/> range was exceeded.</summary>
    Overflow,
}

/// <summary>User-facing text for calculator errors.</summary>
public static class CalculatorErrorText
{
    /// <summary>Returns the display text for an error.</summary>
    public static string ToDisplayText(this CalculatorError error) => error switch
    {
        CalculatorError.None => string.Empty,
        CalculatorError.DivisionByZero => "Cannot divide by zero",
        CalculatorError.Overflow => "Number too large",
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unknown error."),
    };
}
