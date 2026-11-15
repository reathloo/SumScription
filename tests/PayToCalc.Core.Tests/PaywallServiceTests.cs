using PayToCalc.Core.Paywall;

namespace PayToCalc.Core.Tests;

public sealed class PaywallServiceTests
{
    [Fact]
    public void FreeEvaluations_AreVisibleUpToEight()
    {
        var service = new PaywallService(new FakePaywallStateStore());

        for (var i = 1; i <= PaywallService.FreeEvaluations; i++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation(), $"Auswertung {i} muss sichtbar sein.");
        }

        Assert.False(service.RegisterSuccessfulEvaluation());
        Assert.False(service.IsResultVisible);
        Assert.Equal(9, service.State.SuccessfulEvaluations);
    }

    [Fact]
    public void EveryEvaluation_IsPersisted()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        service.RegisterSuccessfulEvaluation();
        service.RegisterSuccessfulEvaluation();

        Assert.Equal(2, store.State.SuccessfulEvaluations);
        Assert.Equal(2, store.SaveCount);
    }

    [Fact]
    public void Unlock_MakesResultVisibleAndStoresPlan()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        for (var i = 0; i < 12; i++)
        {
            service.RegisterSuccessfulEvaluation();
        }

        Assert.False(service.IsResultVisible);

        service.Unlock(PaywallPlan.Subscription);

        Assert.True(service.IsResultVisible);
        Assert.True(store.State.IsUnlocked);
        Assert.Equal(PaywallPlan.Subscription, store.State.Plan);
    }

    [Fact]
    public void UnlockWithNone_DoesNothing()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        service.Unlock(PaywallPlan.None);

        Assert.False(service.State.IsUnlocked);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void Reset_ResetsCounterUnlockAndPlan()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        service.RegisterSuccessfulEvaluation();
        service.Unlock(PaywallPlan.OneTime);

        service.Reset();

        Assert.Equal(0, service.State.SuccessfulEvaluations);
        Assert.False(service.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, service.State.Plan);
        Assert.True(service.IsResultVisible);
        Assert.Equal(PaywallState.Default, store.State);
    }

    [Theory]
    [InlineData(PaywallPlan.Subscription)]
    [InlineData(PaywallPlan.OneTime)]
    public void Plan_SurvivesReload(PaywallPlan plan)
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);
        service.Unlock(plan);

        var reloaded = new PaywallService(store);

        Assert.True(reloaded.State.IsUnlocked);
        Assert.Equal(plan, reloaded.State.Plan);
        Assert.True(reloaded.IsResultVisible);
    }

    [Fact]
    public void UndefinedPlan_IsTreatedAsDefect()
    {
        var store = new FakePaywallStateStore { State = new PaywallState(9, true, (PaywallPlan)7) };

        var service = new PaywallService(store);

        Assert.Equal(PaywallState.Default, service.State);
        Assert.Equal(PaywallPlan.None, service.State.Plan);
        Assert.False(service.State.IsUnlocked);
        Assert.True(service.IsResultVisible);
    }

    [Fact]
    public void NegativeCounter_IsNormalized()
    {
        var store = new FakePaywallStateStore { State = new PaywallState(-5, false, PaywallPlan.Subscription) };

        var service = new PaywallService(store);

        Assert.Equal(0, service.State.SuccessfulEvaluations);
        Assert.Equal(PaywallPlan.None, service.State.Plan);
        Assert.Equal(PaywallService.FreeEvaluations, service.RemainingFreeEvaluations);
    }
}
