namespace SumScription.Core.Paywall;

/// <summary>Free quota, result unlock, and fictional premium credits.</summary>
public sealed class PaywallService
{
    /// <summary>Earliest successful evaluation that may trigger the paywall.</summary>
    public const int MinimumPaywallTriggerEvaluation = 4;

    /// <summary>Latest successful evaluation that may trigger the paywall.</summary>
    public const int MaximumPaywallTriggerEvaluation = 7;

    /// <summary>Deterministic free quota used by the default constructor and tests.</summary>
    public const int FreeEvaluations = MaximumPaywallTriggerEvaluation - 1;

    private readonly IPaywallStateStore _store;
    private int _successfulEvaluationsThisRun;

    /// <summary>Creates the service with the latest supported paywall trigger.</summary>
    public PaywallService(IPaywallStateStore store)
        : this(store, MaximumPaywallTriggerEvaluation)
    {
    }

    /// <summary>Creates the service and loads persisted state.</summary>
    public PaywallService(IPaywallStateStore store, int paywallTriggerEvaluation)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (paywallTriggerEvaluation is < MinimumPaywallTriggerEvaluation or > MaximumPaywallTriggerEvaluation)
        {
            throw new ArgumentOutOfRangeException(nameof(paywallTriggerEvaluation));
        }

        _store = store;
        PaywallTriggerEvaluation = paywallTriggerEvaluation;
        var loaded = Normalize(store.Load());
        State = loaded with
        {
            IsUnlocked = false,
            Plan = PaywallPlan.None,
            PremiumPercentCredits = 0,
        };

        if (State != loaded)
        {
            _store.Save(State);
        }
    }

    /// <summary>The current normalized state.</summary>
    public PaywallState State { get; private set; }

    /// <summary>The successful evaluation in this app run that opens the paywall.</summary>
    public int PaywallTriggerEvaluation { get; }

    /// <summary>Remaining free evaluations, never negative.</summary>
    public int RemainingFreeEvaluations => Math.Max(0, PaywallTriggerEvaluation - 1 - _successfulEvaluationsThisRun);

    /// <summary>Whether evaluated results may remain visible.</summary>
    public bool IsResultVisible => State.IsUnlocked || _successfulEvaluationsThisRun < PaywallTriggerEvaluation;

    /// <summary>Registers a successful evaluation and returns whether its result is visible.</summary>
    public bool RegisterSuccessfulEvaluation()
    {
        _successfulEvaluationsThisRun++;
        State = State with { SuccessfulEvaluations = State.SuccessfulEvaluations + 1 };
        SavePersistentState();
        return IsResultVisible;
    }

    /// <summary>Fictionally unlocks results and stores the selected plan.</summary>
    public void Unlock(PaywallPlan plan)
    {
        if (plan == PaywallPlan.None)
        {
            return;
        }

        State = plan == PaywallPlan.SingleCalculation
            ? State with { IsUnlocked = false, Plan = plan }
            : State with { IsUnlocked = true, Plan = plan };
    }

    /// <summary>Adds the credits from a fictional premium percentage pack.</summary>
    public void PurchasePremiumPercentPack(PremiumPercentPack pack)
    {
        if (!Enum.IsDefined(pack))
        {
            return;
        }

        var credits = Math.Min(
            int.MaxValue,
            (long)State.PremiumPercentCredits + (int)pack);
        State = State with { PremiumPercentCredits = (int)credits };
    }

    /// <summary>Consumes one premium percentage credit when available.</summary>
    public bool TryConsumePremiumPercentCredit()
    {
        if (State.PremiumPercentCredits <= 0)
        {
            return false;
        }

        State = State with { PremiumPercentCredits = State.PremiumPercentCredits - 1 };
        return true;
    }

    /// <summary>Resets quotas, unlocks, plans, and premium credits.</summary>
    public void Reset()
    {
        _successfulEvaluationsThisRun = 0;
        State = PaywallState.Default;
        _store.Save(State);
    }

    private void SavePersistentState() =>
        _store.Save(State with
        {
            IsUnlocked = false,
            Plan = PaywallPlan.None,
            PremiumPercentCredits = 0,
        });

    private static PaywallState Normalize(PaywallState state)
    {
        // An undefined plan can only originate from a defective state source.
        if (!Enum.IsDefined(state.Plan))
        {
            return PaywallState.Default;
        }

        return new PaywallState(
            Math.Max(0, state.SuccessfulEvaluations),
            state.IsUnlocked,
            state.IsUnlocked ? state.Plan : PaywallPlan.None,
            Math.Max(0, state.PremiumPercentCredits));
    }
}
