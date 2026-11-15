using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SumScription.App.ViewModels;
using SumScription.App.Views;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.App;

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
