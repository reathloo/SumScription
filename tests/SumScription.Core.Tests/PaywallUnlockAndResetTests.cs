using System.Text.Json;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class PaywallUnlockAndResetTests : IDisposable
{
    private readonly string _directory;
    private readonly string _path;

    public PaywallUnlockAndResetTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "sumscription-tests-" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "state.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(PaywallPlan.Monthly)]
    [InlineData(PaywallPlan.Annual)]
    public void Unlock_AfterMaskedEvaluation_RevealsResult_UntilReload(PaywallPlan plan)
    {
        var service = new PaywallService(new JsonFilePaywallStateStore(_path));
        ExhaustFreeQuota(service);

        Assert.False(service.IsResultVisible);

        service.Unlock(plan);

        Assert.True(service.IsResultVisible);
        Assert.True(service.State.IsUnlocked);
        Assert.Equal(plan, service.State.Plan);
        Assert.True(service.RegisterSuccessfulEvaluation());

        var reloaded = new PaywallService(new JsonFilePaywallStateStore(_path));

        Assert.False(reloaded.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, reloaded.State.Plan);
        Assert.True(reloaded.IsResultVisible);
        Assert.Equal(PaywallService.FreeEvaluations + 2, reloaded.State.SuccessfulEvaluations);
    }

    [Theory]
    [InlineData(PaywallPlan.Monthly)]
    [InlineData(PaywallPlan.Annual)]
    public void Session_UnlockAfterMaskedResult_RevealsCurrentResultAndHistory(PaywallPlan plan)
    {
        var paywall = new PaywallService(new JsonFilePaywallStateStore(_path));
        var session = new CalculatorSession(paywall);

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations + 1; evaluation++)
        {
            Assert.True(Evaluate(session, "3*3"));
        }

        Assert.True(session.IsResultMasked);
        Assert.True(session.History[0].IsMasked);
        Assert.Equal("9", session.History[0].Value);

        session.Unlock(plan);

        Assert.False(session.IsResultMasked);
        Assert.All(session.History, entry => Assert.False(entry.IsMasked));

        var reloaded = new CalculatorSession(new PaywallService(new JsonFilePaywallStateStore(_path)));

        Assert.True(reloaded.Paywall.IsResultVisible);
        Assert.False(reloaded.Paywall.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, reloaded.Paywall.State.Plan);
    }

    [Fact]
    public void ResetAfterPurchase_ResetsCounterUnlockAndPlan_Persisted()
    {
        var service = new PaywallService(new JsonFilePaywallStateStore(_path));
        ExhaustFreeQuota(service);
        service.Unlock(PaywallPlan.Monthly);

        Assert.True(service.State.IsUnlocked);

        service.Reset();

        Assert.Equal(PaywallState.Default, service.State);
        Assert.Equal(0, service.State.SuccessfulEvaluations);
        Assert.False(service.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, service.State.Plan);
        Assert.True(service.IsResultVisible);
        Assert.Equal(PaywallService.FreeEvaluations, service.RemainingFreeEvaluations);

        using var document = JsonDocument.Parse(File.ReadAllText(_path));
        Assert.Equal(0, document.RootElement.GetProperty("SuccessfulEvaluations").GetInt32());
        Assert.False(document.RootElement.GetProperty("IsUnlocked").GetBoolean());
        Assert.Equal("None", document.RootElement.GetProperty("Plan").GetString());
        Assert.False(File.Exists(_path + ".tmp"));

        var reloaded = new PaywallService(new JsonFilePaywallStateStore(_path));

        Assert.Equal(PaywallState.Default, reloaded.State);
        Assert.True(reloaded.IsResultVisible);
        Assert.Equal(PaywallService.FreeEvaluations, reloaded.RemainingFreeEvaluations);
    }

    [Fact]
    public void Session_ResetAfterPurchase_MasksAtTheTriggerAgain()
    {
        var paywall = new PaywallService(new JsonFilePaywallStateStore(_path));
        var session = new CalculatorSession(paywall);
        ExhaustFreeQuota(paywall);
        session.Unlock(PaywallPlan.Annual);

        session.ResetDemo();

        Assert.False(session.IsResultMasked);
        Assert.Equal(0, session.Paywall.State.SuccessfulEvaluations);
        Assert.Equal(PaywallPlan.None, session.Paywall.State.Plan);

        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(Evaluate(session, "1+1"));
            Assert.False(session.IsResultMasked);
        }

        Assert.True(Evaluate(session, "2+2"));
        Assert.True(session.IsResultMasked);
    }

    [Fact]
    public void PremiumPercentCredits_AreClearedOnReload()
    {
        var service = new PaywallService(new JsonFilePaywallStateStore(_path));
        service.PurchasePremiumPercentPack(PremiumPercentPack.Ten);
        Assert.True(service.TryConsumePremiumPercentCredit());

        var reloaded = new PaywallService(new JsonFilePaywallStateStore(_path));
        Assert.Equal(0, reloaded.State.PremiumPercentCredits);
    }

    private static void ExhaustFreeQuota(PaywallService service)
    {
        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation(), $"Evaluation {evaluation} must remain visible.");
        }

        Assert.False(service.RegisterSuccessfulEvaluation());
        Assert.Equal(PaywallService.FreeEvaluations + 1, service.State.SuccessfulEvaluations);
    }

    private static bool Evaluate(CalculatorSession session, string input)
    {
        session.ClearAll();
        Input.Enter(session, input);
        return session.Evaluate();
    }
}
