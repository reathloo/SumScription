using SumScription.App.ViewModels;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class PaywallViewModelTests
{
    [Fact]
    public void Paywall_AppearsOnlyAfterQuota_AndNeedsThreeDismissals()
    {
        var viewModel = CreateViewModel();

        EvaluateSuccessfully(viewModel, PaywallService.FreeEvaluations);

        Assert.False(viewModel.IsPaywallVisible);
        Assert.False(viewModel.CanReopenPaywall);

        EvaluateSuccessfully(viewModel, 1);

        Assert.True(viewModel.IsPaywallVisible);
        Assert.Equal("Noch 3 Ablehnungen bis zurück zum Rechner", viewModel.PaywallDismissalHint);

        viewModel.ClosePaywall();
        Assert.True(viewModel.IsPaywallVisible);
        Assert.Equal("Noch 2 Ablehnungen bis zurück zum Rechner", viewModel.PaywallDismissalHint);

        viewModel.ClosePaywall();
        Assert.True(viewModel.IsPaywallVisible);
        Assert.Equal("Noch 1 Ablehnung bis zurück zum Rechner", viewModel.PaywallDismissalHint);

        viewModel.ClosePaywall();
        Assert.False(viewModel.IsPaywallVisible);
        Assert.False(viewModel.IsOverlayOpen);
        Assert.True(viewModel.CanReopenPaywall);
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
        Assert.Equal("Dein Ergebnis ist fertig.", viewModel.PaywallTitle);
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
