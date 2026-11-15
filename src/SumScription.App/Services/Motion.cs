using Avalonia;
using Avalonia.Animation;
using Avalonia.VisualTree;

namespace SumScription.App.Services;

/// <summary>Schaltet Bewegungen ab, wenn die Umgebung sie reduziert.</summary>
public static class Motion
{
    /// <summary>Umgebungsvariable für reduzierte Bewegung.</summary>
    public const string ReducedMotionVariable = "SUMSCRIPTION_REDUCED_MOTION";

    /// <summary>Wahr, wenn nur Opazitätswechsel ohne Verschieben erlaubt sind.</summary>
    public static bool Reduced { get; } = ReadReducedMotion();

    /// <summary>Ersetzt alle Übergänge durch 1-ms-Opazitätswechsel.</summary>
    public static void DisableTransitions(Visual root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (root is Animatable animatable)
        {
            animatable.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(1),
                },
            };
        }

        foreach (var child in root.GetVisualChildren())
        {
            DisableTransitions(child);
        }
    }

    private static bool ReadReducedMotion()
    {
        var value = Environment.GetEnvironmentVariable(ReducedMotionVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim() is "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
