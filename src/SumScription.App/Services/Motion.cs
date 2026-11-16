using Avalonia;
using Avalonia.Animation;
using Avalonia.VisualTree;

namespace SumScription.App.Services;

/// <summary>Reduces motion when requested by the environment.</summary>
public static class Motion
{
    /// <summary>Environment variable used to request reduced motion.</summary>
    public const string ReducedMotionVariable = "SUMSCRIPTION_REDUCED_MOTION";

    /// <summary>Whether only opacity feedback without movement is allowed.</summary>
    public static bool Reduced { get; } = ReadReducedMotion();

    /// <summary>Replaces transitions with one-millisecond opacity changes.</summary>
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
