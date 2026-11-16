namespace SumScription.Core.Paywall;

/// <summary>Persistence contract for parody paywall state.</summary>
public interface IPaywallStateStore
{
    /// <summary>Loads state and must not throw.</summary>
    PaywallState Load();

    /// <summary>Saves state and must not throw.</summary>
    void Save(PaywallState state);
}
