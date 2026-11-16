using System.Text.Json;
using System.Text.Json.Serialization;

namespace SumScription.Core.Paywall;

/// <summary>Persists parody state as JSON using atomic replacement.</summary>
public sealed class JsonFilePaywallStateStore : IPaywallStateStore
{
    private const string FileName = "state.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) },
    };

    /// <summary>Creates a store for a fixed file path.</summary>
    public JsonFilePaywallStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    /// <summary>Path to the state file.</summary>
    public string FilePath { get; }

    /// <summary>Default path inside the user's application data directory.</summary>
    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SumScription",
        FileName);

    /// <summary>Creates the store in the application data directory.</summary>
    public static JsonFilePaywallStateStore CreateDefault() => new(DefaultFilePath);

    /// <inheritdoc />
    public PaywallState Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return PaywallState.Default;
            }

            var json = File.ReadAllText(FilePath);
            var state = JsonSerializer.Deserialize<PaywallState>(json, SerializerOptions);

            // An undefined plan is a defective file, not valid state.
            return state is null || !Enum.IsDefined(state.Plan) ? PaywallState.Default : state;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return PaywallState.Default;
        }
    }

    /// <inheritdoc />
    public void Save(PaywallState state)
    {
        var tempPath = FilePath + ".tmp";

        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tempPath, JsonSerializer.Serialize(state, SerializerOptions));
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // A failed write must never block the parody.
            DeleteTempFile(tempPath);
        }
    }

    // Cleans up a leftover temporary file; this step must never throw either.
    private static void DeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Cleanup is best effort.
        }
    }
}
