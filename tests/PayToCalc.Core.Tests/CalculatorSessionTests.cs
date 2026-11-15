using PayToCalc.Core.Calculator;
using PayToCalc.Core.Paywall;

namespace PayToCalc.Core.Tests;

public sealed class CalculatorSessionTests
{
    private static CalculatorSession CreateSession(out FakePaywallStateStore store)
    {
        store = new FakePaywallStateStore();
        return new CalculatorSession(new PaywallService(store));
    }

    [Fact]
    public void NinthEvaluation_IsMasked()
    {
        var session = CreateSession(out _);

        for (var i = 0; i < PaywallService.FreeEvaluations; i++)
        {
            Evaluate(session, "1+1");
            Assert.False(session.IsResultMasked, $"Auswertung {i + 1} muss sichtbar sein.");
            Assert.Equal("2", session.ResultText);
        }

        Evaluate(session, "2+2");

        Assert.True(session.IsResultMasked);
        Assert.Equal("4", session.ResultText);
        Assert.Equal("2 + 2", session.ExpressionText);
    }

    [Fact]
    public void Error_DoesNotCountAsEvaluation()
    {
        var session = CreateSession(out var store);

        session.InputDigit('1');
        session.InputOperator('/');
        session.InputDigit('0');

        Assert.False(session.Evaluate());

        Assert.Equal(0, store.State.SuccessfulEvaluations);
        Assert.Equal("Division durch null", session.ResultText);
        Assert.Empty(session.History);
    }

    [Fact]
    public void Unlock_ShowsMaskedResult()
    {
        var session = CreateSession(out _);

        for (var i = 0; i <= PaywallService.FreeEvaluations; i++)
        {
            Evaluate(session, "3*3");
        }

        Assert.True(session.IsResultMasked);
        Assert.True(session.History[0].IsMasked);
        Assert.False(session.History[1].IsMasked);

        session.Unlock(PaywallPlan.OneTime);

        Assert.False(session.IsResultMasked);
        Assert.All(session.History, entry => Assert.False(entry.IsMasked));
        Assert.Equal(PaywallPlan.OneTime, session.Paywall.State.Plan);
    }

    [Fact]
    public void ResetDemo_ClearsCounterAndMask()
    {
        var session = CreateSession(out var store);

        for (var i = 0; i <= PaywallService.FreeEvaluations; i++)
        {
            Evaluate(session, "5+5");
        }

        session.Unlock(PaywallPlan.Subscription);
        session.ResetDemo();

        Assert.Equal(0, store.State.SuccessfulEvaluations);
        Assert.Equal(PaywallPlan.None, store.State.Plan);
        Assert.False(session.IsResultMasked);
        Assert.False(session.Paywall.State.IsUnlocked);
    }

    [Fact]
    public void History_KeepsAtMostTwentyEntries_NewestFirst()
    {
        var session = CreateSession(out _);

        for (var i = 1; i <= 25; i++)
        {
            session.ClearAll();
            session.InputDigit('1');
            session.InputOperator('+');
            session.InputDigit((char)('0' + (i % 10)));
            Assert.True(session.Evaluate());
        }

        Assert.Equal(CalculatorSession.HistoryCapacity, session.History.Count);
        Assert.Equal("1 + 5", session.History[0].Expression);
        Assert.Equal("1 + 6", session.History[^1].Expression);
    }

    [Fact]
    public void History_IsNotClearedByClearAll()
    {
        var session = CreateSession(out _);

        Evaluate(session, "6+6");
        session.ClearAll();

        Assert.Single(session.History);
        Assert.Equal("6 + 6", session.History[0].Expression);
        Assert.Equal("12", session.History[0].Result);
    }

    private static void Evaluate(CalculatorSession session, string input)
    {
        session.ClearAll();
        Input.Enter(session, input);
        Assert.True(session.Evaluate());
    }
}
