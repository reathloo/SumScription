using PayToCalc.Core.Calculator;
using PayToCalc.Core.Paywall;

namespace PayToCalc.Core.Tests;

internal sealed class FakePaywallStateStore : IPaywallStateStore
{
    public PaywallState State { get; set; } = PaywallState.Default;

    public int SaveCount { get; private set; }

    public PaywallState Load() => State;

    public void Save(PaywallState state)
    {
        State = state;
        SaveCount++;
    }
}

internal static class Input
{
    public static void Enter(CalculatorEngine engine, string text)
    {
        foreach (var character in text)
        {
            if (char.IsAsciiDigit(character))
            {
                engine.InputDigit(character);
            }
            else if (character is ',' or '.')
            {
                engine.InputDecimalSeparator();
            }
            else
            {
                engine.InputOperator(character);
            }
        }
    }

    public static void Enter(CalculatorSession session, string text)
    {
        foreach (var character in text)
        {
            if (char.IsAsciiDigit(character))
            {
                session.InputDigit(character);
            }
            else if (character is ',' or '.')
            {
                session.InputDecimalSeparator();
            }
            else
            {
                session.InputOperator(character);
            }
        }
    }
}
