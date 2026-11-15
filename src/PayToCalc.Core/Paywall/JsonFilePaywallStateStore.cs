using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayToCalc.Core.Paywall;

/// <summary>Speichert den Demo-Zustand als JSON-Datei und schreibt atomar.</summary>
public sealed class JsonFilePaywallStateStore : IPaywallStateStore
{
    private const string FileName = "state.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Erzeugt den Store für einen festen Dateipfad.</summary>
    public JsonFilePaywallStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    /// <summary>Der Pfad der Zustandsdatei.</summary>
    public string FilePath { get; }

    /// <summary>Der Standardpfad im Anwendungsdatenverzeichnis des Benutzers.</summary>
    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PayToCalc",
        FileName);

    /// <summary>Erzeugt den Store im Anwendungsdatenverzeichnis.</summary>
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
            return JsonSerializer.Deserialize<PaywallState>(json, SerializerOptions) ?? PaywallState.Default;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return PaywallState.Default;
        }
    }

    /// <inheritdoc />
    public void Save(PaywallState state)
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(state, SerializerOptions));
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Ein fehlgeschlagener Schreibvorgang darf die Demo nicht blockieren.
        }
    }
}
