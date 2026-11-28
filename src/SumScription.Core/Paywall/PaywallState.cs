namespace SumScription.Core.Paywall;

/// <summary>Persisted state for the parody paywall.</summary>
/// <param name="SuccessfulEvaluations">Total successful evaluations.</param>
/// <param name="IsUnlocked">Whether the result paywall was fictionally unlocked.</param>
/// <param name="Plan">The unlocked result plan.</param>
/// <param name="PremiumPercentCredits">Unused premium percentage calculations.</param>
/// <param name="AppLaunchCount">Number of recorded application launches.</param>
/// <param name="NextUpdateNoticeLaunch">Launch that should show the next fake update notice.</param>
public sealed record PaywallState(
    int SuccessfulEvaluations,
    bool IsUnlocked,
    PaywallPlan Plan,
    int PremiumPercentCredits = 0,
    int AppLaunchCount = 0,
    int NextUpdateNoticeLaunch = 0)
{
    /// <summary>State for a fresh installation.</summary>
    public static PaywallState Default { get; } = new(0, false, PaywallPlan.None, 0, 0, 0);
}
