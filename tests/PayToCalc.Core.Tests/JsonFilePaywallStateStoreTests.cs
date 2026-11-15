using PayToCalc.Core.Paywall;

namespace PayToCalc.Core.Tests;

public sealed class JsonFilePaywallStateStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _path;

    public JsonFilePaywallStateStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "paytocalc-tests-" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "state.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void MissingFile_ReturnsDefaultState()
    {
        var store = new JsonFilePaywallStateStore(_path);

        var state = store.Load();

        Assert.Equal(PaywallState.Default, state);
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void CorruptJson_ReturnsDefaultState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "{ das ist kein json");

        var store = new JsonFilePaywallStateStore(_path);

        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void JsonWithWrongTypes_ReturnsDefaultState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, """{ "SuccessfulEvaluations": "viele", "IsUnlocked": true }""");

        var store = new JsonFilePaywallStateStore(_path);

        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void Save_CreatesDirectoryAndFile()
    {
        var store = new JsonFilePaywallStateStore(_path);

        store.Save(new PaywallState(3, false, PaywallPlan.None));

        Assert.True(File.Exists(_path));
        Assert.False(File.Exists(_path + ".tmp"));
        Assert.Equal(new PaywallState(3, false, PaywallPlan.None), store.Load());
    }

    [Theory]
    [InlineData(PaywallPlan.Subscription)]
    [InlineData(PaywallPlan.OneTime)]
    public void ChosenPlan_IsPersistedAndReloaded(PaywallPlan plan)
    {
        var service = new PaywallService(new JsonFilePaywallStateStore(_path));

        service.RegisterSuccessfulEvaluation();
        service.Unlock(plan);

        var reloaded = new PaywallService(new JsonFilePaywallStateStore(_path));

        Assert.True(reloaded.State.IsUnlocked);
        Assert.Equal(plan, reloaded.State.Plan);
        Assert.Equal(1, reloaded.State.SuccessfulEvaluations);
        Assert.True(reloaded.IsResultVisible);
    }

    [Fact]
    public void Reset_IsPersisted()
    {
        var path = _path;
        var service = new PaywallService(new JsonFilePaywallStateStore(path));
        service.Unlock(PaywallPlan.Subscription);

        service.Reset();

        var reloaded = new PaywallService(new JsonFilePaywallStateStore(path));

        Assert.False(reloaded.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, reloaded.State.Plan);
    }

    [Fact]
    public void UnwritablePath_DoesNotThrow()
    {
        var store = new JsonFilePaywallStateStore(Path.Combine(_path, "tiefer", "state.json"));

        var exception = Record.Exception(() => store.Save(new PaywallState(1, false, PaywallPlan.None)));

        Assert.Null(exception);
    }

    [Fact]
    public void DefaultPath_IsInsideApplicationData()
    {
        var store = JsonFilePaywallStateStore.CreateDefault();

        Assert.Contains("PayToCalc", store.FilePath, StringComparison.Ordinal);
        Assert.EndsWith("state.json", store.FilePath, StringComparison.Ordinal);
    }
}
