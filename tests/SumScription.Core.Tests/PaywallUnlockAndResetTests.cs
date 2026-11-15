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
    [InlineData(PaywallPlan.Subscription)]
    [InlineData(PaywallPlan.OneTime)]
    public void Unlock_AfterMaskedEvaluation_RevealsResult_AndPlanSurvivesReload(PaywallPlan plan)
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

        Assert.True(reloaded.State.IsUnlocked);
        Assert.Equal(plan, reloaded.State.Plan);
        Assert.True(reloaded.IsResultVisible);
        Assert.Equal(PaywallService.FreeEvaluations + 2, reloaded.State.SuccessfulEvaluations);
    }

    [Theory]
    [InlineData(PaywallPlan.Subscription)]
    [InlineData(PaywallPlan.OneTime)]
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
        Assert.Equal(plan, reloaded.Paywall.State.Plan);
    }

    [Fact]
    public void ResetAfterPurchase_ResetsCounterUnlockAndPlan_Persisted()
    {
        var service = new PaywallService(new JsonFilePaywallStateStore(_path));
        ExhaustFreeQuota(service);
        service.Unlock(PaywallPlan.Subscription);

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
    public void Session_ResetAfterPurchase_MasksTheNextNinthEvaluationAgain()
    {
        var paywall = new PaywallService(new JsonFilePaywallStateStore(_path));
        var session = new CalculatorSession(paywall);
        ExhaustFreeQuota(paywall);
        session.Unlock(PaywallPlan.OneTime);

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

    private static void ExhaustFreeQuota(PaywallService service)
    {
        for (var evaluation = 1; evaluation <= PaywallService.FreeEvaluations; evaluation++)
        {
            Assert.True(service.RegisterSuccessfulEvaluation(), $"Auswertung {evaluation} muss sichtbar sein.");
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
