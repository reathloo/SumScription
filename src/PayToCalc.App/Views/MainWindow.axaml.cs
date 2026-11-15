using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PayToCalc.App.Services;
using PayToCalc.App.ViewModels;

namespace PayToCalc.App.Views;

/// <summary>Fenster mit Rechenband, Tastenfeld und Demo-Paywall.</summary>
public partial class MainWindow : Window
{
    private const int KeypadRows = 5;
    private const int KeypadColumns = 5;
    private const double KeyHeightCompact = 46d;
    private const double KeyHeightRoomy = 52d;
    private const double RoomyWindowHeight = 720d;

    private readonly Control?[,] _keypad = new Control?[KeypadRows, KeypadColumns];

    private MainWindowViewModel? _viewModel;
    private IInputElement? _previousFocus;

    /// <summary>Erzeugt das Fenster ohne Datenkontext.</summary>
    public MainWindow()
    {
        InitializeComponent();
        MapKeypad();
        SizeChanged += OnWindowSizeChanged;
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>Erzeugt das Fenster für ein ViewModel.</summary>
    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        Attach(viewModel);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_viewModel is { } viewModel && !e.Handled)
        {
            if (viewModel.IsCheckoutVisible)
            {
                if (e.Key == Key.Escape)
                {
                    viewModel.CancelCheckout();
                    e.Handled = true;
                    return;
                }

                base.OnKeyDown(e);
                return;
            }

            if (viewModel.IsPaywallVisible)
            {
                if (e.Key == Key.Escape)
                {
                    viewModel.ClosePaywall();
                    e.Handled = true;
                }
                else
                {
                    base.OnKeyDown(e);
                }

                return;
            }

            if (HandleCalculatorKey(viewModel, e))
            {
                e.Handled = true;
                return;
            }

            if (MoveFocusInKeypad(e.Key))
            {
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (Motion.Reduced)
        {
            Motion.DisableTransitions(this);
        }

        AcButton.Focus();
    }

    // Einzige Stelle, die Enter auswertet: der Tunnel-Handler läuft vor allen Knöpfen und
    // markiert die Geste, damit weder ein fokussierter Knopf noch OnKeyDown sie erneut auslöst.
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_viewModel is not { } viewModel || viewModel.IsOverlayOpen)
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            return;
        }

        if (e.Key is Key.Enter or Key.Return)
        {
            viewModel.Evaluate();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        SizeChanged -= OnWindowSizeChanged;

        if (_viewModel is { } viewModel)
        {
            viewModel.Evaluated -= OnEvaluated;
            viewModel.ErrorAppeared -= OnErrorAppeared;
            viewModel.PaywallOpened -= OnPaywallOpened;
            viewModel.CheckoutOpened -= OnCheckoutOpened;
            viewModel.CheckoutClosed -= OnCheckoutClosed;
            viewModel.OverlaysClosed -= OnOverlaysClosed;
            _viewModel = null;
        }

        base.OnClosed(e);
    }

    private void Attach(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.Evaluated += OnEvaluated;
        viewModel.ErrorAppeared += OnErrorAppeared;
        viewModel.PaywallOpened += OnPaywallOpened;
        viewModel.CheckoutOpened += OnCheckoutOpened;
        viewModel.CheckoutClosed += OnCheckoutClosed;
        viewModel.OverlaysClosed += OnOverlaysClosed;
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var keyHeight = e.NewSize.Height >= RoomyWindowHeight ? KeyHeightRoomy : KeyHeightCompact;
        foreach (var row in Keypad.RowDefinitions)
        {
            if (Math.Abs(row.Height.Value - keyHeight) > 0.1d)
            {
                row.Height = new GridLength(keyHeight);
            }
        }
    }

    private void MapKeypad()
    {
        foreach (var child in Keypad.Children)
        {
            if (child is not Control control)
            {
                continue;
            }

            var row = Grid.GetRow(control);
            var column = Grid.GetColumn(control);
            var rowSpan = Math.Max(1, Grid.GetRowSpan(control));
            var columnSpan = Math.Max(1, Grid.GetColumnSpan(control));

            for (var r = row; r < row + rowSpan && r < KeypadRows; r++)
            {
                for (var c = column; c < column + columnSpan && c < KeypadColumns; c++)
                {
                    _keypad[r, c] = control;
                }
            }
        }
    }

    private static bool HandleCalculatorKey(MainWindowViewModel viewModel, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            return false;
        }

        // Layoutabhängige Zeichen zuerst: auf deutscher Tastatur ist Shift+0 „=“, Shift+7 „/“,
        // Shift+Plus „*“ — physische Key-Werte wären hier falsch.
        return HandleSymbol(viewModel, e.KeySymbol) || HandlePhysicalKey(viewModel, e.Key);
    }

    private static bool HandleSymbol(MainWindowViewModel viewModel, string? symbol)
    {
        if (symbol is not { Length: 1 })
        {
            return false;
        }

        var character = symbol[0];
        if (char.IsAsciiDigit(character))
        {
            viewModel.TypeDigit(character);
            return true;
        }

        switch (character)
        {
            case '+':
            case '-':
            case '*':
            case '/':
                viewModel.TypeOperator(character);
                return true;
            case ',':
            case '.':
                viewModel.TypeSeparator();
                return true;
            case '=':
                viewModel.Evaluate();
                return true;
            default:
                return false;
        }
    }

    // Nur Aktionen ohne layoutabhängiges Zeichen: Ziffernblock, Escape, Rücktaste.
    private static bool HandlePhysicalKey(MainWindowViewModel viewModel, Key key)
    {
        switch (key)
        {
            case >= Key.NumPad0 and <= Key.NumPad9:
                viewModel.TypeDigit((char)('0' + (key - Key.NumPad0)));
                return true;
            case Key.Decimal:
                viewModel.TypeSeparator();
                return true;
            case Key.Add:
                viewModel.TypeOperator('+');
                return true;
            case Key.Subtract:
                viewModel.TypeOperator('-');
                return true;
            case Key.Multiply:
                viewModel.TypeOperator('*');
                return true;
            case Key.Divide:
                viewModel.TypeOperator('/');
                return true;
            case Key.Escape:
                viewModel.ClearAll();
                return true;
            case Key.Back:
                viewModel.Backspace();
                return true;
            default:
                return false;
        }
    }

    private bool MoveFocusInKeypad(Key key)
    {
        if (key is not (Key.Left or Key.Right or Key.Up or Key.Down))
        {
            return false;
        }

        if (FocusManager?.GetFocusedElement() is not Control focused)
        {
            return false;
        }

        for (var r = 0; r < KeypadRows; r++)
        {
            for (var c = 0; c < KeypadColumns; c++)
            {
                if (!ReferenceEquals(_keypad[r, c], focused))
                {
                    continue;
                }

                var (dr, dc) = key switch
                {
                    Key.Left => (0, -1),
                    Key.Right => (0, 1),
                    Key.Up => (-1, 0),
                    _ => (1, 0),
                };

                var targetRow = Math.Clamp(r + dr, 0, KeypadRows - 1);
                var targetColumn = Math.Clamp(c + dc, 0, KeypadColumns - 1);
                _keypad[targetRow, targetColumn]?.Focus();
                return true;
            }
        }

        return false;
    }

    private void OnPaywallScrimPressed(object? sender, PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (ReferenceEquals(e.Source, sender))
        {
            _viewModel?.ClosePaywall();
        }
    }

    private void OnEvaluated(object? sender, EventArgs e) => Dispatcher.UIThread.Post(AnimateResult);

    private void OnErrorAppeared(object? sender, EventArgs e) => Dispatcher.UIThread.Post(AnimateError);

    private void OnPaywallOpened(object? sender, EventArgs e)
    {
        _previousFocus = FocusManager?.GetFocusedElement();
        Dispatcher.UIThread.Post(() =>
        {
            AnimateIn(PaywallSheet);
            PlanSubscriptionButton.Focus();
        });
    }

    private void OnCheckoutOpened(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            AnimateIn(CheckoutPanel);
            CheckoutConfirmButton.Focus();
        });

    private void OnCheckoutClosed(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel?.SelectedPlan == Core.Paywall.PaywallPlan.OneTime)
            {
                PlanOneTimeButton.Focus();
            }
            else
            {
                PlanSubscriptionButton.Focus();
            }
        });

    private void OnOverlaysClosed(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            var previous = _previousFocus;
            _previousFocus = null;
            if (previous is Control { IsEffectivelyVisible: true, IsEffectivelyEnabled: true })
            {
                previous.Focus();
                return;
            }

            // Nach dem Kauf ist „Ergebnis freischalten" ausgeblendet — dann zurück auf AC.
            (UnlockButton.IsEffectivelyVisible ? UnlockButton : AcButton).Focus();
        });

    private void AnimateResult()
    {
        if (Motion.Reduced)
        {
            return;
        }

        var targetOpacity = _viewModel?.IsResultMasked == true ? 0.35d : 1d;
        Run(ResultLine, 160, targetOpacity, 4);
    }

    private void AnimateError()
    {
        if (Motion.Reduced)
        {
            return;
        }

        Run(ErrorLine, 160, 1d, 4);
        var shake = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(160),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                KeyFrame(0, 0, 0),
                KeyFrame(0.5, 3, 1),
                KeyFrame(1, 0, 1),
            },
        };
        _ = shake.RunAsync(ErrorLine);
    }

    private void AnimateIn(Control control)
    {
        if (Motion.Reduced)
        {
            control.Opacity = 1;
            return;
        }

        control.RenderTransform = new TranslateTransform();
        control.Opacity = 1;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(180),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0d),
                        new Setter(TranslateTransform.YProperty, 12d),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1d),
                        new Setter(TranslateTransform.YProperty, 0d),
                    },
                },
            },
        };

        _ = animation.RunAsync(control);
    }

    private static void Run(Control control, int milliseconds, double opacity, double offset)
    {
        var transform = control.RenderTransform as TranslateTransform ?? new TranslateTransform();
        control.RenderTransform = transform;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0d),
                        new Setter(TranslateTransform.YProperty, offset),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(OpacityProperty, opacity),
                        new Setter(TranslateTransform.YProperty, 0d),
                    },
                },
            },
        };

        _ = animation.RunAsync(control);
    }

    private static KeyFrame KeyFrame(double cue, double offsetX, double opacity) => new()
    {
        Cue = new Cue(cue),
        Setters =
        {
            new Setter(OpacityProperty, opacity),
            new Setter(TranslateTransform.XProperty, offsetX),
        },
    };
}
