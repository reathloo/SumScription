using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SumScription.App.Services;
using SumScription.App.ViewModels;
using SumScription.App.Views;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.App;

/// <summary>Starts the application window and calculator session.</summary>
public sealed class App : Application
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var triggerEvaluation = Random.Shared.Next(
                PaywallService.MinimumPaywallTriggerEvaluation,
                PaywallService.MaximumPaywallTriggerEvaluation + 1);
            var session = new CalculatorSession(
                new PaywallService(JsonFilePaywallStateStore.CreateDefault(), triggerEvaluation));
            var viewModel = new MainWindowViewModel(session);
            desktop.MainWindow = new MainWindow(viewModel);
            var bundledIconPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "Resources", "app-icon.png"));
            var developmentIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app-icon.png");
            MacDockIcon.TryApply(File.Exists(bundledIconPath) ? bundledIconPath : developmentIconPath);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
