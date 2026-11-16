using System.Text.Json;
using SumScription.Core.Paywall;

namespace SumScription.Core.Tests;

public sealed class JsonFilePaywallStateStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _path;

    public JsonFilePaywallStateStoreTests()
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
        File.WriteAllText(_path, "{ this is not json");

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
    [InlineData(PaywallPlan.Monthly)]
    [InlineData(PaywallPlan.Annual)]
    public void ChosenPlan_IsClearedWhenTheServiceReloads(PaywallPlan plan)
    {
        var service = new PaywallService(new JsonFilePaywallStateStore(_path));

        service.RegisterSuccessfulEvaluation();
        service.Unlock(plan);

        var reloaded = new PaywallService(new JsonFilePaywallStateStore(_path));

        Assert.False(reloaded.State.IsUnlocked);
        Assert.Equal(PaywallPlan.None, reloaded.State.Plan);
        Assert.Equal(1, reloaded.State.SuccessfulEvaluations);
        Assert.True(reloaded.IsResultVisible);
    }

    [Fact]
    public void Reset_IsPersisted()
    {
        var path = _path;
        var service = new PaywallService(new JsonFilePaywallStateStore(path));
        service.Unlock(PaywallPlan.Monthly);

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

        Assert.Contains("SumScription", store.FilePath, StringComparison.Ordinal);
        Assert.EndsWith("state.json", store.FilePath, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyFile_ReturnsDefaultState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, string.Empty);

        var store = new JsonFilePaywallStateStore(_path);

        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void JsonNull_ReturnsDefaultState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "null");

        var store = new JsonFilePaywallStateStore(_path);

        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void JsonWithArrayRoot_ReturnsDefaultState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "[1, 2, 3]");

        var store = new JsonFilePaywallStateStore(_path);

        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void JsonWithUnknownPlanName_ReturnsDefaultState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, """{ "SuccessfulEvaluations": 9, "IsUnlocked": true, "Plan": "Lifetime" }""");

        var store = new JsonFilePaywallStateStore(_path);

        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void JsonWithNumericPlan_ReturnsDefaultState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, """{ "SuccessfulEvaluations": 9, "IsUnlocked": true, "Plan": 7 }""");

        var store = new JsonFilePaywallStateStore(_path);

        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void NumericPlan_YieldsDefaultServiceState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, """{ "SuccessfulEvaluations": 9, "IsUnlocked": true, "Plan": 7 }""");

        var service = new PaywallService(new JsonFilePaywallStateStore(_path));

        Assert.Equal(PaywallState.Default, service.State);
        Assert.Equal(PaywallPlan.None, service.State.Plan);
        Assert.True(service.IsResultVisible);
    }

    [Fact]
    public void UnknownPlanName_YieldsDefaultServiceState()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, """{ "SuccessfulEvaluations": 2, "IsUnlocked": true, "Plan": "Unfug" }""");

        var service = new PaywallService(new JsonFilePaywallStateStore(_path));

        Assert.Equal(PaywallState.Default, service.State);
        Assert.True(service.IsResultVisible);
    }

    [Fact]
    public void StatePathIsDirectory_ReturnsDefaultStateWithoutException()
    {
        Directory.CreateDirectory(_path);

        var store = new JsonFilePaywallStateStore(_path);

        var exception = Record.Exception(() => store.Load());

        Assert.Null(exception);
        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void ParentPathIsFile_ReturnsDefaultStateAndSaveDoesNotThrow()
    {
        Directory.CreateDirectory(_directory);
        var blockingFile = Path.Combine(_directory, "blockiert");
        File.WriteAllText(blockingFile, "not a directory");
        var store = new JsonFilePaywallStateStore(Path.Combine(blockingFile, "state.json"));

        Assert.Equal(PaywallState.Default, store.Load());
        Assert.Null(Record.Exception(() => store.Save(new PaywallState(2, false, PaywallPlan.None))));
        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void Save_WritesValidJsonWithoutTempFileLeftBehind()
    {
        var store = new JsonFilePaywallStateStore(_path);

        store.Save(new PaywallState(3, true, PaywallPlan.Monthly));

        Assert.False(File.Exists(_path + ".tmp"));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));

        using var document = JsonDocument.Parse(File.ReadAllText(_path));
        Assert.Equal(3, document.RootElement.GetProperty("SuccessfulEvaluations").GetInt32());
        Assert.True(document.RootElement.GetProperty("IsUnlocked").GetBoolean());
        Assert.Equal("Monthly", document.RootElement.GetProperty("Plan").GetString());
    }

    [Fact]
    public void Save_ReplacesExistingStateCompletely()
    {
        var store = new JsonFilePaywallStateStore(_path);
        store.Save(new PaywallState(25, true, PaywallPlan.Annual));

        store.Save(PaywallState.Default);

        var json = File.ReadAllText(_path);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(0, document.RootElement.GetProperty("SuccessfulEvaluations").GetInt32());
        Assert.False(document.RootElement.GetProperty("IsUnlocked").GetBoolean());
        Assert.Equal("None", document.RootElement.GetProperty("Plan").GetString());
        Assert.DoesNotContain("Annual", json, StringComparison.Ordinal);
        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void Save_WhenTargetPathIsDirectory_RemovesTempFileAndKeepsNothingBehind()
    {
        Directory.CreateDirectory(_path);
        var store = new JsonFilePaywallStateStore(_path);

        var exception = Record.Exception(() => store.Save(new PaywallState(1, false, PaywallPlan.None)));

        Assert.Null(exception);
        Assert.False(File.Exists(_path + ".tmp"));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.Equal(PaywallState.Default, store.Load());
    }

    [Fact]
    public void Save_WhenTargetPathIsDirectory_KeepsExistingState()
    {
        var store = new JsonFilePaywallStateStore(_path);
        store.Save(new PaywallState(4, false, PaywallPlan.None));

        // A directory at the target path makes the move fail.
        Directory.Move(_path, Path.Combine(_directory, "state.json.bak"));
        Directory.CreateDirectory(_path);

        var exception = Record.Exception(() => store.Save(new PaywallState(9, true, PaywallPlan.Monthly)));

        Assert.Null(exception);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.True(File.Exists(Path.Combine(_directory, "state.json.bak")));
    }

    [Fact]
    public void Save_WhenTempPathIsBlocked_KeepsPreviousStateIntact()
    {
        var store = new JsonFilePaywallStateStore(_path);
        store.Save(new PaywallState(4, false, PaywallPlan.None));
        var previous = File.ReadAllText(_path);

        Directory.CreateDirectory(_path + ".tmp");

        var exception = Record.Exception(() => store.Save(new PaywallState(9, true, PaywallPlan.Monthly)));

        Assert.Null(exception);
        Assert.Equal(previous, File.ReadAllText(_path));
        Assert.Equal(new PaywallState(4, false, PaywallPlan.None), store.Load());
    }
}
