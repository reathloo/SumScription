namespace SumScription.Core.Paywall;

/// <summary>Freikontingent und Freischaltung der Demo.</summary>
public sealed class PaywallService
{
    /// <summary>Erfolgreiche Auswertungen des Freikontingents.</summary>
    public const int FreeEvaluations = 8;

    private readonly IPaywallStateStore _store;

    /// <summary>Erzeugt den Dienst und lädt den gespeicherten Zustand.</summary>
    public PaywallService(IPaywallStateStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        State = Normalize(store.Load());
    }

    /// <summary>Der aktuelle, normalisierte Zustand.</summary>
    public PaywallState State { get; private set; }

    /// <summary>Verbleibende kostenlose Auswertungen (nie negativ).</summary>
    public int RemainingFreeEvaluations => Math.Max(0, FreeEvaluations - State.SuccessfulEvaluations);

    /// <summary>Wahr, wenn das Ergebnis sichtbar bleiben darf.</summary>
    public bool IsResultVisible => State.IsUnlocked || State.SuccessfulEvaluations <= FreeEvaluations;

    /// <summary>Zählt eine erfolgreiche Auswertung und liefert, ob das Ergebnis sichtbar ist.</summary>
    public bool RegisterSuccessfulEvaluation()
    {
        State = State with { SuccessfulEvaluations = State.SuccessfulEvaluations + 1 };
        _store.Save(State);
        return IsResultVisible;
    }

    /// <summary>Schaltet dauerhaft frei und speichert den Tarif.</summary>
    public void Unlock(PaywallPlan plan)
    {
        if (plan == PaywallPlan.None)
        {
            return;
        }

        State = State with { IsUnlocked = true, Plan = plan };
        _store.Save(State);
    }

    /// <summary>Setzt Zähler, Freischaltung und Tarif zurück.</summary>
    public void Reset()
    {
        State = PaywallState.Default;
        _store.Save(State);
    }

    private static PaywallState Normalize(PaywallState state)
    {
        // Defensiv: ein nicht definierter Tarif kann nur aus einer defekten Quelle stammen.
        if (!Enum.IsDefined(state.Plan))
        {
            return PaywallState.Default;
        }

        return new PaywallState(
            Math.Max(0, state.SuccessfulEvaluations),
            state.IsUnlocked,
            state.IsUnlocked ? state.Plan : PaywallPlan.None);
    }
}
