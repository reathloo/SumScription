namespace PayToCalc.Core.Paywall;

/// <summary>Gespeicherter Zustand der Demo-Paywall.</summary>
/// <param name="SuccessfulEvaluations">Erfolgreiche Auswertungen insgesamt.</param>
/// <param name="IsUnlocked">Wahr, wenn per Demo-Kauf freigeschaltet.</param>
/// <param name="Plan">Der freigeschaltete Tarif.</param>
public sealed record PaywallState(int SuccessfulEvaluations, bool IsUnlocked, PaywallPlan Plan)
{
    /// <summary>Der Zustand einer frischen Installation.</summary>
    public static PaywallState Default { get; } = new(0, false, PaywallPlan.None);
}
