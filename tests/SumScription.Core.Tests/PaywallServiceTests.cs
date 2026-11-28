using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class PaywallServiceTests
{
    [Fact]
    public void FakeUpdateNotice_AppearsOnScheduledLaunchAndReschedules()
    {
        var service = new PaywallService(new FakePaywallStateStore());

        Assert.False(service.RegisterLaunch(3));
        Assert.False(service.RegisterLaunch(3));
        Assert.True(service.RegisterLaunch(3));
        Assert.False(service.RegisterLaunch(3));
        Assert.False(service.RegisterLaunch(3));
        Assert.True(service.RegisterLaunch(3));

        Assert.Equal(6, service.State.AppLaunchCount);
        Assert.Equal(9, service.State.NextUpdateNoticeLaunch);
    }

    [Fact]
    public void FakeUpdateNoticeSchedule_SurvivesServiceReload()
    {
        var store = new FakePaywallStateStore();
        var firstRun = new PaywallService(store);

        Assert.False(firstRun.RegisterLaunch(2));

        var secondRun = new PaywallService(store);

        Assert.True(secondRun.RegisterLaunch(4));
        Assert.Equal(2, secondRun.State.AppLaunchCount);
        Assert.Equal(6, secondRun.State.NextUpdateNoticeLaunch);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void FakeUpdateNotice_RejectsIntervalsOutsideTwoToFourLaunches(int interval)
    {
        var service = new PaywallService(new FakePaywallStateStore());

        Assert.Throws<ArgumentOutOfRangeException>(() => service.RegisterLaunch(interval));
    }

    [Fact]
    public void FreeEvaluations_AreVisibleUntilConfiguredTrigger()
    {
        var service = new PaywallService(new FakePaywallStateStore());

        for (var i = 1; i <= PaywallService.FreeEvaluations; i++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation(), $"Evaluation {i} must remain visible.");
        }

        Assert.False(service.RegisterSuccessfulEvaluation());
        Assert.False(service.IsResultVisible);
        Assert.Equal(PaywallService.MaximumPaywallTriggerEvaluation, service.State.SuccessfulEvaluations);
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

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void ConfiguredTrigger_MasksExactlyThatEvaluation(int triggerEvaluation)
    {
        var service = new PaywallService(new FakePaywallStateStore(), triggerEvaluation);

        for (var evaluation = 1; evaluation < triggerEvaluation; evaluation++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation());
        }

        Assert.False(service.RegisterSuccessfulEvaluation());
        Assert.Equal(triggerEvaluation, service.State.SuccessfulEvaluations);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    public void TriggerOutsideSupportedRange_IsRejected(int triggerEvaluation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PaywallService(new FakePaywallStateStore(), triggerEvaluation));
    }

    [Fact]
    public void Unlock_MakesResultVisibleWithoutPersistingThePurchase()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        for (var i = 0; i < 12; i++)
        {
            service.RegisterSuccessfulEvaluation();
        }

        Assert.False(service.IsResultVisible);

        service.Unlock(PaywallPlan.Monthly);

        Assert.True(service.IsResultVisible);
        Assert.True(service.State.IsUnlocked);
        Assert.Equal(PaywallPlan.Monthly, service.State.Plan);
        Assert.False(store.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, store.State.Plan);
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
    public void SingleCalculation_DoesNotUnlockFutureResultsOrPersist()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);
        for (var i = 0; i < PaywallService.MaximumPaywallTriggerEvaluation; i++)
        {
            service.RegisterSuccessfulEvaluation();
        }

        service.Unlock(PaywallPlan.SingleCalculation);

        Assert.False(service.IsResultVisible);
        Assert.False(service.State.IsUnlocked);
        Assert.Equal(PaywallPlan.SingleCalculation, service.State.Plan);
        Assert.Equal(PaywallPlan.None, store.State.Plan);
    }

    [Fact]
    public void Reset_ResetsCounterUnlockAndPlan()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        service.RegisterSuccessfulEvaluation();
        service.Unlock(PaywallPlan.Annual);

        service.Reset();

        Assert.Equal(0, service.State.SuccessfulEvaluations);
        Assert.False(service.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, service.State.Plan);
        Assert.True(service.IsResultVisible);
        Assert.Equal(PaywallState.Default, store.State);
    }

    [Fact]
    public void Reset_RemovesPremiumPercentCredits()
    {
        var service = new PaywallService(new FakePaywallStateStore());
        service.PurchasePremiumPercentPack(PremiumPercentPack.Hundred);

        service.Reset();

        Assert.Equal(0, service.State.PremiumPercentCredits);
    }

    [Theory]
    [InlineData(PaywallPlan.Monthly)]
    [InlineData(PaywallPlan.Annual)]
    public void FictionalResultPurchase_IsClearedOnReload(PaywallPlan plan)
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);
        service.Unlock(plan);

        var reloaded = new PaywallService(store);

        Assert.False(reloaded.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, reloaded.State.Plan);
        Assert.True(reloaded.IsResultVisible);
    }

    [Fact]
    public void EvaluationPersistsOnlyTheCounterWhilePurchasesStayInMemory()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);
        service.Unlock(PaywallPlan.Annual);
        service.PurchasePremiumPercentPack(PremiumPercentPack.Ten);

        service.RegisterSuccessfulEvaluation();

        Assert.True(service.State.IsUnlocked);
        Assert.Equal(10, service.State.PremiumPercentCredits);
        Assert.Equal(1, store.State.SuccessfulEvaluations);
        Assert.False(store.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, store.State.Plan);
        Assert.Equal(0, store.State.PremiumPercentCredits);

        var reloaded = new PaywallService(store);
        Assert.Equal(1, reloaded.State.SuccessfulEvaluations);
        Assert.False(reloaded.State.IsUnlocked);
        Assert.Equal(0, reloaded.State.PremiumPercentCredits);
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
        var store = new FakePaywallStateStore { State = new PaywallState(-5, false, PaywallPlan.Monthly) };

        var service = new PaywallService(store);

        Assert.Equal(0, service.State.SuccessfulEvaluations);
        Assert.Equal(PaywallPlan.None, service.State.Plan);
        Assert.Equal(PaywallService.FreeEvaluations, service.RemainingFreeEvaluations);
    }

    [Fact]
    public void InvalidPremiumPack_DoesNotChangeOrPersistState()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        service.PurchasePremiumPercentPack((PremiumPercentPack)7);

        Assert.Equal(0, service.State.PremiumPercentCredits);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void NegativePremiumCredits_AreNormalized()
    {
        var store = new FakePaywallStateStore
        {
            State = new PaywallState(0, false, PaywallPlan.None, -5),
        };

        var service = new PaywallService(store);

        Assert.Equal(0, service.State.PremiumPercentCredits);
    }
}
