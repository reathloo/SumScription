namespace SumScription.Core.Paywall;

/// <summary>Selected fictional result-unlock plan.</summary>
public enum PaywallPlan
{
    /// <summary>No plan selected.</summary>
    None,

    /// <summary>Monthly result subscription.</summary>
    Monthly,

    /// <summary>Annual result subscription.</summary>
    Annual,

    /// <summary>One result without an ongoing unlock.</summary>
    SingleCalculation,

    /// <summary>Annual result subscription with one hundred percentage credits.</summary>
    AnnualPremium,

    // Legacy names keep older persisted development state readable.
    Subscription = Monthly,
    OneTime = Annual,
}
