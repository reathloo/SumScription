using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class PaywallQuotaTests
{
    [Fact]
    public void EvaluationsBeforeTrigger_AreVisible_AndCountedAfterEachStep()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation(), $"Evaluation {evaluation} must remain visible.");
            Assert.True(service.IsResultVisible, $"Result must remain visible after evaluation {evaluation}.");
            Assert.Equal(evaluation, service.State.SuccessfulEvaluations);
            Assert.Equal(evaluation, store.State.SuccessfulEvaluations);
            Assert.Equal(PaywallService.FreeEvaluations - evaluation, service.RemainingFreeEvaluations);
        }
    }

    [Fact]
    public void TriggerEvaluation_IsMasked_AndCounterIsPersisted()
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
    public void Session_TriggerResultIsMasked_WhileExpressionStaysReadable()
    {
        var store = new FakePaywallStateStore();
        var session = new CalculatorSession(new PaywallService(store));

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(Evaluate(session, "1+1"), $"Evaluation {evaluation} must succeed.");

            Assert.False(session.IsResultMasked, $"Evaluation {evaluation} must remain visible.");
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
    [InlineData("1/0", "Cannot divide by zero")]
    [InlineData("999999999999999*999999999999999", "Number too large")]
    public void FailedEvaluations_DoNotCount_EvenWhenRepeated(string input, string message)
    {
        var store = new FakePaywallStateStore();
        var session = new CalculatorSession(new PaywallService(store));

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.False(Evaluate(session, input), $"Attempt {attempt + 1} must not count as an evaluation.");

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

            Assert.False(session.IsResultMasked, $"Evaluation {evaluation} must remain visible.");
            Assert.Equal(evaluation, store.State.SuccessfulEvaluations);
        }

        Assert.True(Evaluate(session, "2+2"));

        Assert.True(session.IsResultMasked);
        Assert.Equal(PaywallService.FreeEvaluations + 1, store.State.SuccessfulEvaluations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(500)]
    public void StoredCounter_DoesNotConsumeTheNewRunQuota(int counter)
    {
        var store = new FakePaywallStateStore { State = new PaywallState(counter, false, PaywallPlan.None) };

        var service = new PaywallService(store);

        Assert.True(service.IsResultVisible);
        Assert.Equal(PaywallService.FreeEvaluations, service.RemainingFreeEvaluations);
    }

    [Fact]
    public void Session_AfterReloadWithOldQuotaUsed_StartsFreshRunQuota()
    {
        var store = new FakePaywallStateStore { State = new PaywallState(PaywallService.FreeEvaluations, false, PaywallPlan.None) };
        var session = new CalculatorSession(new PaywallService(store));

        Assert.True(Evaluate(session, "3+3"));

        Assert.False(session.IsResultMasked);
        Assert.Equal(PaywallService.FreeEvaluations + 1, store.State.SuccessfulEvaluations);
    }

    private static bool Evaluate(CalculatorSession session, string input)
    {
        session.ClearAll();
        Input.Enter(session, input);
        return session.Evaluate();
    }
}
