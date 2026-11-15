using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PayToCalc.App.ViewModels;
using PayToCalc.App.Views;
using PayToCalc.Core.Calculator;
using PayToCalc.Core.Paywall;

namespace PayToCalc.App;

/// <summary>Startet Fenster und Sitzung.</summary>
public sealed class App : Application
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var session = new CalculatorSession(new PaywallService(JsonFilePaywallStateStore.CreateDefault()));
            var viewModel = new MainWindowViewModel(session);
            desktop.MainWindow = new MainWindow(viewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
