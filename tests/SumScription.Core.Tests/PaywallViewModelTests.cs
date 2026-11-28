using SumScription.App.ViewModels;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class PaywallViewModelTests
{
    [Fact]
    public void FakeUpdateNotice_CanBeShownAtStartupAndDismissed()
    {
        var viewModel = new MainWindowViewModel(
            new CalculatorSession(new PaywallService(new FakePaywallStateStore())),
            showUpdateNotice: true);

        Assert.True(viewModel.IsUpdateNoticeVisible);
        Assert.True(viewModel.IsOverlayOpen);

        viewModel.DismissUpdateNotice();

        Assert.False(viewModel.IsUpdateNoticeVisible);
        Assert.False(viewModel.IsOverlayOpen);
    }

    [Fact]
    public void TipPrompt_AppearsOnceAndAcceptedTipUsesExistingCelebration()
    {
        var viewModel = new MainWindowViewModel(
            new CalculatorSession(new PaywallService(new FakePaywallStateStore())),
            tipTriggerEvaluation: 2);
        var purchaseEvents = 0;
        viewModel.PurchaseCompleted += (_, _) => purchaseEvents++;

        EvaluateSuccessfully(viewModel, 1);
        Assert.False(viewModel.IsTipVisible);

        EvaluateSuccessfully(viewModel, 1);
        Assert.True(viewModel.IsTipVisible);

        viewModel.SelectTip("40");

        Assert.False(viewModel.IsTipVisible);
        Assert.Equal(1, purchaseEvents);
        Assert.Equal("40% TIP ACCEPTED · YOUR CALCULATOR FEELS SEEN ✨", viewModel.PurchaseCelebrationText);

        EvaluateSuccessfully(viewModel, 2);
        Assert.False(viewModel.IsTipVisible);
    }

    [Fact]
    public void DivisionByZero_OpensEnterpriseNoticeWithoutCountingAsEvaluation()
    {
        var viewModel = CreateViewModel();
        viewModel.TypeDigit('1');
        viewModel.TypeOperator('/');
        viewModel.TypeDigit('0');

        viewModel.Evaluate();

        Assert.True(viewModel.IsDivisionByZeroVisible);
        Assert.True(viewModel.IsOverlayOpen);
        Assert.Equal("Cannot divide by zero", viewModel.DisplayResult);

        viewModel.DismissDivisionByZero();

        Assert.False(viewModel.IsDivisionByZeroVisible);
        Assert.False(viewModel.IsOverlayOpen);
    }

    [Fact]
    public void Paywall_AppearsOnlyAfterQuota_AndNeedsThreeDismissals()
    {
        var viewModel = CreateViewModel();

        EvaluateSuccessfully(viewModel, PaywallService.FreeEvaluations);

        Assert.False(viewModel.IsPaywallVisible);
        Assert.Equal(0d, viewModel.PaywallOpacity);
        Assert.False(viewModel.CanReopenPaywall);

        EvaluateSuccessfully(viewModel, 1);

        Assert.True(viewModel.IsPaywallVisible);
        Assert.False(viewModel.IsShopIconVisible);
        Assert.Equal(1d, viewModel.PaywallOpacity);
        Assert.Equal(0d, viewModel.PremiumPercentOfferOpacity);
        Assert.Equal(1d, viewModel.ResultOfferOpacity);

        viewModel.ClosePaywall();
        Assert.True(viewModel.IsPaywallVisible);

        viewModel.ClosePaywall();
        Assert.True(viewModel.IsPaywallVisible);

        viewModel.ClosePaywall();
        Assert.False(viewModel.IsPaywallVisible);
        Assert.Equal(0d, viewModel.PaywallOpacity);
        Assert.False(viewModel.IsOverlayOpen);
        Assert.True(viewModel.CanReopenPaywall);
        Assert.True(viewModel.IsShopIconVisible);
    }

    [Fact]
    public void ResetDemo_RearmsAutomaticPaywall()
    {
        var viewModel = CreateViewModel();
        EvaluateSuccessfully(viewModel, PaywallService.FreeEvaluations + 1);
        DismissPaywall(viewModel);

        viewModel.ResetDemo();
        EvaluateSuccessfully(viewModel, PaywallService.FreeEvaluations + 1);

        Assert.True(viewModel.IsPaywallVisible);
        Assert.Equal("Your answer is ready.", viewModel.PaywallTitle);
    }

    [Fact]
    public void SingleCalculationOffer_UsesSpecialPriceAndDoesNotUnlockSubscription()
    {
        var viewModel = CreateViewModel();
        EvaluateSuccessfully(viewModel, PaywallService.FreeEvaluations + 1);

        viewModel.SelectPlan(PaywallPlan.SingleCalculation);

        Assert.True(viewModel.IsCheckoutVisible);
        Assert.Equal("1 result · €4.99", viewModel.CheckoutPlanLine);
        Assert.Equal("Special offer · valid for this result only", viewModel.CheckoutNoteLine);

        viewModel.ConfirmPurchase();

        Assert.False(viewModel.IsUnlocked);
        Assert.False(viewModel.IsResultMasked);

        EvaluateSuccessfully(viewModel, 1);
        Assert.True(viewModel.IsResultMasked);
    }

    [Fact]
    public void ResultOffers_SwitchBetweenMonthlyAndAnnualGroups()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.IsMonthlyPlans);
        Assert.False(viewModel.IsAnnualPlans);
        Assert.Equal(1d, viewModel.MonthlyPlansOpacity);
        Assert.Equal(0d, viewModel.AnnualPlansOpacity);

        viewModel.ShowAnnualPlansCommand.Execute(null);

        Assert.False(viewModel.IsMonthlyPlans);
        Assert.True(viewModel.IsAnnualPlans);
        Assert.Equal(0d, viewModel.MonthlyPlansOpacity);
        Assert.Equal(1d, viewModel.AnnualPlansOpacity);

        viewModel.SelectPlan(PaywallPlan.Annual);
        Assert.Equal("Annual Standard · €499 per year", viewModel.CheckoutPlanLine);
        Assert.Equal("One year of premium results without percentage credits", viewModel.CheckoutNoteLine);

        viewModel.CancelPurchaseCommand.Execute(null);
        viewModel.ShowMonthlyPlansCommand.Execute(null);
        viewModel.SelectPlan(PaywallPlan.Monthly);
        Assert.Equal("Monthly Ultra · €74.99 per month", viewModel.CheckoutPlanLine);
    }

    [Fact]
    public void AnnualPremiumPurchase_UnlocksResultsAndAddsOneHundredPercentCredits()
    {
        var viewModel = CreateViewModel();
        EvaluateSuccessfully(viewModel, PaywallService.FreeEvaluations + 1);

        viewModel.SelectPlan(PaywallPlan.AnnualPremium);

        Assert.Equal("Annual Premium · €699 · includes 100 % calculations", viewModel.CheckoutPlanLine);
        Assert.Equal("One year of results plus 100 premium percentage credits", viewModel.CheckoutNoteLine);

        viewModel.ConfirmPurchase();

        Assert.True(viewModel.IsUnlocked);
        Assert.False(viewModel.IsResultMasked);
        Assert.Equal(PaywallPlan.AnnualPremium, viewModel.SelectedPlan);
        Assert.Equal(100, viewModel.PremiumPercentCredits);
        Assert.Equal("ANNUAL PREMIUM + 100 % CREDITS ACQUIRED ✨", viewModel.PurchaseCelebrationText);
    }

    private static MainWindowViewModel CreateViewModel() =>
        new(new CalculatorSession(new PaywallService(new FakePaywallStateStore())));

    private static void EvaluateSuccessfully(MainWindowViewModel viewModel, int count)
    {
        for (var evaluation = 0; evaluation < count; evaluation++)
        {
            viewModel.ClearAll();
            viewModel.TypeDigit('1');
            viewModel.TypeOperator('+');
            viewModel.TypeDigit('1');
            viewModel.Evaluate();
        }
    }

    private static void DismissPaywall(MainWindowViewModel viewModel)
    {
        viewModel.ClosePaywall();
        viewModel.ClosePaywall();
        viewModel.ClosePaywall();
    }
}
