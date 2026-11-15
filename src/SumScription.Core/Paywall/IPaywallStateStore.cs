namespace SumScription.Core.Paywall;

/// <summary>Persistenz der Demo-Paywall.</summary>
public interface IPaywallStateStore
{
    /// <summary>Lädt den Zustand; darf nicht werfen.</summary>
    PaywallState Load();

    /// <summary>Speichert den Zustand; darf nicht werfen.</summary>
    void Save(PaywallState state);
}
