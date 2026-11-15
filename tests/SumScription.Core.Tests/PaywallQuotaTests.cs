using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class PaywallQuotaTests
{
    [Fact]
    public void FirstEightEvaluations_AreVisible_AndCountedAfterEachStep()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation(), $"Auswertung {evaluation} muss sichtbar sein.");
            Assert.True(service.IsResultVisible, $"Nach Auswertung {evaluation} muss das Ergebnis sichtbar sein.");
            Assert.Equal(evaluation, service.State.SuccessfulEvaluations);
            Assert.Equal(evaluation, store.State.SuccessfulEvaluations);
            Assert.Equal(PaywallService.FreeEvaluations - evaluation, service.RemainingFreeEvaluations);
        }
    }

    [Fact]
    public void NinthEvaluation_IsMasked_AndCounterReachesNine()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation());
        }

        Assert.False(service.RegisterSuccessfulEvaluation());

        Assert.False(service.IsResultVisible);
        Assert.Equal(PaywallService.FreeEvaluations + 1, service.State.SuccessfulEvaluations);
        Assert.Equal(PaywallService.FreeEvaluations + 1, store.State.SuccessfulEvaluations);
        Assert.Equal(0, service.RemainingFreeEvaluations);
        Assert.Equal(PaywallService.FreeEvaluations + 1, store.SaveCount);
    }

    [Fact]
    public void Session_NinthResultIsMasked_WhileExpressionStaysReadable()
    {
        var store = new FakePaywallStateStore();
        var session = new CalculatorSession(new PaywallService(store));

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(Evaluate(session, "1+1"), $"Auswertung {evaluation} muss erfolgreich sein.");

            Assert.False(session.IsResultMasked, $"Auswertung {evaluation} muss sichtbar sein.");
            Assert.Equal("1 + 1", session.ExpressionText);
            Assert.Equal("2", session.DisplayResult);
            Assert.Equal(evaluation, store.State.SuccessfulEvaluations);
        }

        Assert.True(Evaluate(session, "2+2"));

        Assert.True(session.IsResultMasked);
        Assert.Equal("2 + 2", session.ExpressionText);
        Assert.Equal(PaywallService.FreeEvaluations + 1, store.State.SuccessfulEvaluations);
        Assert.True(session.History[0].IsMasked);
        Assert.False(session.Paywall.IsResultVisible);
    }

    [Theory]
    [InlineData("1/0", "Division durch null")]
    [InlineData("999999999999999*999999999999999", "Zahl zu groß")]
    public void FailedEvaluations_DoNotCount_EvenWhenRepeated(string input, string message)
    {
        var store = new FakePaywallStateStore();
        var session = new CalculatorSession(new PaywallService(store));

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.False(Evaluate(session, input), $"Versuch {attempt + 1} darf nicht als Auswertung zählen.");

            Assert.True(session.HasError);
            Assert.Equal(message, session.DisplayResult);
            Assert.Equal(0, store.State.SuccessfulEvaluations);
            Assert.Equal(0, store.SaveCount);
            Assert.Empty(session.History);
            Assert.False(session.IsResultMasked);
        }
    }

    [Fact]
    public void ErrorsBetweenSuccesses_DoNotConsumeTheQuota()
    {
        var store = new FakePaywallStateStore();
        var session = new CalculatorSession(new PaywallService(store));

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.False(Evaluate(session, "1/0"));
            Assert.False(Evaluate(session, "999999999999999*999999999999999"));
            Assert.True(Evaluate(session, "1+1"));

            Assert.False(session.IsResultMasked, $"Auswertung {evaluation} muss sichtbar sein.");
            Assert.Equal(evaluation, store.State.SuccessfulEvaluations);
        }

        Assert.True(Evaluate(session, "2+2"));

        Assert.True(session.IsResultMasked);
        Assert.Equal(PaywallService.FreeEvaluations + 1, store.State.SuccessfulEvaluations);
    }

    [Theory]
    [InlineData(0, true, 8)]
    [InlineData(7, true, 1)]
    [InlineData(8, true, 0)]
    [InlineData(9, false, 0)]
    [InlineData(500, false, 0)]
    public void StoredCounter_DecidesVisibilityAfterReload(int counter, bool visible, int remaining)
    {
        var store = new FakePaywallStateStore { State = new PaywallState(counter, false, PaywallPlan.None) };

        var service = new PaywallService(store);

        Assert.Equal(visible, service.IsResultVisible);
        Assert.Equal(remaining, service.RemainingFreeEvaluations);
    }

    [Fact]
    public void Session_AfterReloadWithFullQuotaUsed_MasksTheNextResult()
    {
        var store = new FakePaywallStateStore { State = new PaywallState(PaywallService.FreeEvaluations, false, PaywallPlan.None) };
        var session = new CalculatorSession(new PaywallService(store));

        Assert.True(Evaluate(session, "3+3"));

        Assert.True(session.IsResultMasked);
        Assert.Equal(PaywallService.FreeEvaluations + 1, store.State.SuccessfulEvaluations);
    }

    private static bool Evaluate(CalculatorSession session, string input)
    {
        session.ClearAll();
        Input.Enter(session, input);
        return session.Evaluate();
    }
}
