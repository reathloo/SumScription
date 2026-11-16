using SumScription.App.ViewModels;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class PremiumPercentFeatureTests
{
    [Fact]
    public void PremiumPack_LastOnlyForTheCurrentServiceInstance()
    {
        var store = new FakePaywallStateStore();
        var service = new PaywallService(store);

        service.PurchasePremiumPercentPack(PremiumPercentPack.Ten);

        Assert.Equal(10, service.State.PremiumPercentCredits);
        Assert.True(service.TryConsumePremiumPercentCredit());
        Assert.Equal(9, service.State.PremiumPercentCredits);
        Assert.Equal(0, store.SaveCount);

        var reloaded = new PaywallService(store);
        Assert.Equal(0, reloaded.State.PremiumPercentCredits);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void PercentageOperation_RequiresAndConsumesOneCredit()
    {
        var service = new PaywallService(new FakePaywallStateStore());
        var session = new CalculatorSession(service);
        session.InputDigit('5');

        Assert.False(session.TryPercent());
        Assert.Equal("5", session.DisplayResult);

        service.PurchasePremiumPercentPack(PremiumPercentPack.One);

        Assert.True(session.TryPercent());
        Assert.Equal("0,05", session.DisplayResult);
        Assert.Equal(0, service.State.PremiumPercentCredits);
        Assert.False(session.TryPercent());
    }

    [Fact]
    public void PercentageOperation_DoesNotConsumeCreditWhileAnErrorIsVisible()
    {
        var service = new PaywallService(new FakePaywallStateStore());
        var session = new CalculatorSession(service);
        session.InputDigit('1');
        session.InputOperator('/');
        session.InputDigit('0');
        Assert.False(session.Evaluate());
        service.PurchasePremiumPercentPack(PremiumPercentPack.One);

        Assert.False(session.TryPercent());

        Assert.Equal("Cannot divide by zero", session.DisplayResult);
        Assert.Equal(1, service.State.PremiumPercentCredits);
    }

    [Fact]
    public void PercentageOperation_CannotUseOrRevealAMaskedResult()
    {
        var service = new PaywallService(new FakePaywallStateStore());
        var session = new CalculatorSession(service);
        for (var i = 0; i <= PaywallService.FreeEvaluations; i++)
        {
            session.ClearAll();
            session.InputDigit('5');
            Assert.True(session.Evaluate());
        }

        service.PurchasePremiumPercentPack(PremiumPercentPack.One);

        Assert.False(session.TryPercent());
        Assert.True(session.IsResultMasked);
        Assert.Equal("5", session.DisplayResult);
        Assert.Equal(1, service.State.PremiumPercentCredits);
    }

    [Fact]
    public void PercentageButton_OpensPremiumPaywallAndPurchaseCompletesPendingOperation()
    {
        var viewModel = CreateViewModel();
        var purchaseEvents = 0;
        viewModel.PurchaseCompleted += (_, _) => purchaseEvents++;
        viewModel.TypeDigit('5');

        viewModel.Percent();

        Assert.True(viewModel.IsPaywallVisible);
        Assert.True(viewModel.IsPremiumPercentPaywall);
        Assert.Equal(1d, viewModel.PremiumPercentOfferOpacity);
        Assert.Equal(0d, viewModel.ResultOfferOpacity);
        Assert.Equal("Percent is a premium feature.", viewModel.PaywallTitle);

        viewModel.SelectPremiumPercentPack(PremiumPercentPack.Ten);
        Assert.True(viewModel.IsCheckoutVisible);
        Assert.Equal("10 percentage calculations · €69.99", viewModel.CheckoutPlanLine);

        viewModel.ConfirmPurchase();

        Assert.False(viewModel.IsOverlayOpen);
        Assert.Equal("0,05", viewModel.DisplayResult);
        Assert.Equal(9, viewModel.PremiumPercentCredits);
        Assert.Equal("PREMIUM % UNLOCKED · 9 CREDITS LEFT ✨", viewModel.PurchaseCelebrationText);
        Assert.Equal(1, purchaseEvents);

        viewModel.Percent();
        Assert.Equal("0,0005", viewModel.DisplayResult);
        Assert.Equal(8, viewModel.PremiumPercentCredits);
    }

    [Fact]
    public void RejectingPremiumPercentThreeTimes_DoesNotChangeTheEntry()
    {
        var viewModel = CreateViewModel();
        viewModel.TypeDigit('5');
        viewModel.Percent();

        viewModel.ClosePaywall();
        Assert.Equal("I reject premium division", viewModel.PaywallCloseText);
        viewModel.ClosePaywall();
        Assert.Equal("Keep my percentage locked", viewModel.PaywallCloseText);
        viewModel.ClosePaywall();

        Assert.False(viewModel.IsOverlayOpen);
        Assert.Equal("5", viewModel.DisplayResult);
        Assert.Equal(0, viewModel.PremiumPercentCredits);
    }

    private static MainWindowViewModel CreateViewModel() =>
        new(new CalculatorSession(new PaywallService(new FakePaywallStateStore())));
}
