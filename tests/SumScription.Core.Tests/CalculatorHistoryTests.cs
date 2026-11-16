using System.Globalization;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class CalculatorHistoryTests
{
    [Fact]
    public void TwentyFirstEvaluation_DisplacesTheOldestEntry()
    {
        var session = CreateUnlockedSession();

        for (var value = 1; value <= 21; value++)
        {
            Assert.True(Evaluate(session, "1+" + value.ToString(CultureInfo.InvariantCulture)));
        }

        Assert.Equal(CalculatorSession.HistoryCapacity, session.History.Count);
        Assert.Equal("1 + 21", session.History[0].Expression);
        Assert.Equal("22", session.History[0].Value);
        Assert.Equal("1 + 2", session.History[^1].Expression);
        Assert.Equal("3", session.History[^1].Value);
        Assert.DoesNotContain(session.History, entry => entry.Expression == "1 + 1");
        Assert.All(session.History, entry => Assert.False(entry.IsMasked));
    }

    [Fact]
    public void History_KeepsExactlyTwentyEntries_NotMore()
    {
        var session = CreateUnlockedSession();

        for (var evaluation = 1; evaluation <= 20; evaluation++)
        {
            Assert.True(Evaluate(session, "7*2"));
        }

        Assert.Equal(CalculatorSession.HistoryCapacity, session.History.Count);
        Assert.Equal("14", session.History[0].Value);
    }

    [Fact]
    public void History_IsNotPersisted_AndStartsEmptyInANewSession()
    {
        var store = new FakePaywallStateStore();
        var session = new CalculatorSession(new PaywallService(store));

        Assert.True(Evaluate(session, "6+6"));
        Assert.Single(session.History);
        Assert.Equal(1, store.State.SuccessfulEvaluations);

        var next = new CalculatorSession(new PaywallService(store));

        Assert.Empty(next.History);
        Assert.Equal(1, next.Paywall.State.SuccessfulEvaluations);
    }

    private static CalculatorSession CreateUnlockedSession()
    {
        // Unlock results so the history limit can be tested independently of the free quota.
        var service = new PaywallService(new FakePaywallStateStore());
        service.Unlock(PaywallPlan.Annual);
        return new CalculatorSession(service);
    }

    private static bool Evaluate(CalculatorSession session, string input)
    {
        session.ClearAll();
        Input.Enter(session, input);
        return session.Evaluate();
    }
}
