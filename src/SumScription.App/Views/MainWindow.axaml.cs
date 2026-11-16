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
using SumScription.App.Services;
using SumScription.App.ViewModels;

namespace SumScription.App.Views;

/// <summary>Window containing the calculator, keypad, and parody paywall.</summary>
public partial class MainWindow : Window
{
    private const int KeypadRows = 5;
    private const int KeypadColumns = 4;
    private const double KeyHeightCompact = 60d;
    private const double KeyHeightRoomy = 66d;
    private const double RoomyWindowHeight = 760d;

    private readonly Control?[,] _keypad = new Control?[KeypadRows, KeypadColumns];
    private readonly Random _paywallButtonRandom = new();

    private MainWindowViewModel? _viewModel;
    private IInputElement? _previousFocus;
    private long _lastPaywallButtonMoveAt;

    /// <summary>Creates the window without a data context.</summary>
    public MainWindow()
    {
        InitializeComponent();
        MapKeypad();
        SizeChanged += OnWindowSizeChanged;
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>Creates the window for a view model.</summary>
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

    // The only place that evaluates Enter: this tunnel handler runs before all buttons and
    // marks the gesture so neither a focused button nor OnKeyDown can trigger it twice.
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
            viewModel.PaywallDismissalAdvanced -= OnPaywallDismissalAdvanced;
            viewModel.CheckoutOpened -= OnCheckoutOpened;
            viewModel.CheckoutClosed -= OnCheckoutClosed;
            viewModel.PurchaseCompleted -= OnPurchaseCompleted;
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
        viewModel.PaywallDismissalAdvanced += OnPaywallDismissalAdvanced;
        viewModel.CheckoutOpened += OnCheckoutOpened;
        viewModel.CheckoutClosed += OnCheckoutClosed;
        viewModel.PurchaseCompleted += OnPurchaseCompleted;
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

        // Prefer layout-dependent symbols: German layouts map Shift+0 to "=", Shift+7 to "/",
        // and Shift+Plus to "*", so physical key values would be incorrect.
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
            case '%':
                viewModel.Percent();
                return true;
            default:
                return false;
        }
    }

    // Only actions without layout-dependent symbols: numeric keypad, Escape, and Backspace.
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
            MovePaywallCloseButton(e.GetPosition(PaywallLayer));
            e.Handled = true;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void OnEvaluated(object? sender, EventArgs e) => Dispatcher.UIThread.Post(AnimateResult);

    private void OnErrorAppeared(object? sender, EventArgs e) => Dispatcher.UIThread.Post(AnimateError);

    private void OnPaywallOpened(object? sender, EventArgs e)
    {
        _previousFocus = FocusManager?.GetFocusedElement();
        _lastPaywallButtonMoveAt = 0;
        PaywallCloseButton.RenderTransform = new TranslateTransform();
        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel?.IsPremiumPercentPaywall == true)
            {
                PercentPackTenButton.Focus();
            }
            else
            {
                PlanMonthlyButton.Focus();
            }
        });
    }

    private void OnPaywallDismissalAdvanced(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(AnimatePaywallNag);

    private void OnCheckoutOpened(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            AnimateIn(CheckoutPanel);
            CheckoutConfirmButton.Focus();
        });

    private void OnCheckoutClosed(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel?.IsPremiumPercentPaywall == true)
            {
                switch (_viewModel.SelectedPremiumPercentPack)
                {
                    case Core.Paywall.PremiumPercentPack.One:
                        PercentPackOneButton.Focus();
                        break;
                    case Core.Paywall.PremiumPercentPack.Hundred:
                        PercentPackHundredButton.Focus();
                        break;
                    default:
                        PercentPackTenButton.Focus();
                        break;
                }
            }
            else if (_viewModel?.SelectedPlan == Core.Paywall.PaywallPlan.SingleCalculation)
            {
                PlanSingleCalculationButton.Focus();
            }
            else if (_viewModel?.SelectedPlan == Core.Paywall.PaywallPlan.Annual)
            {
                PlanAnnualButton.Focus();
            }
            else if (_viewModel?.SelectedPlan == Core.Paywall.PaywallPlan.AnnualPremium)
            {
                PlanAnnualPremiumButton.Focus();
            }
            else
            {
                PlanMonthlyButton.Focus();
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

            // After a result purchase the reopen button is hidden, so fall back to AC.
            (UnlockButton.IsEffectivelyVisible ? UnlockButton : AcButton).Focus();
        });

    private void OnPaywallClosePointerEntered(object? sender, PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MovePaywallCloseButton(e.GetPosition(PaywallLayer));
    }

    private void OnPaywallClosePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MovePaywallCloseButton(e.GetPosition(PaywallLayer));
        e.Handled = true;
    }

    private void OnPaywallCloseKeyDown(object? sender, KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.Key is not (Key.Enter or Key.Return or Key.Space))
        {
            return;
        }

        _viewModel?.ClosePaywall();
        e.Handled = true;
    }

    private void MovePaywallCloseButton(Point pointerPosition)
    {
        if (_viewModel?.IsPaywallVisible != true)
        {
            return;
        }

        var now = Environment.TickCount64;
        if (now - _lastPaywallButtonMoveAt < 220)
        {
            return;
        }

        _lastPaywallButtonMoveAt = now;

        var transform = PaywallCloseButton.RenderTransform as TranslateTransform ?? new TranslateTransform();
        var startX = transform.X;
        var startY = transform.Y;
        var layerWidth = Math.Max(PaywallLayer.Bounds.Width, Bounds.Width);
        var layerHeight = Math.Max(PaywallLayer.Bounds.Height, Bounds.Height);
        var buttonWidth = Math.Max(PaywallCloseButton.Bounds.Width, 235d);
        var buttonHeight = Math.Max(PaywallCloseButton.Bounds.Height, 42d);
        var maxX = Math.Max(0d, (layerWidth - buttonWidth) / 2d - 12d);
        var maxUp = Math.Max(0d, layerHeight - buttonHeight - 36d);
        var baseCenterX = layerWidth / 2d;
        var baseCenterY = layerHeight - 18d - buttonHeight / 2d;

        double targetX;
        double targetY;
        var attempts = 0;
        do
        {
            targetX = (_paywallButtonRandom.NextDouble() * 2d - 1d) * maxX;
            targetY = -_paywallButtonRandom.NextDouble() * maxUp;
            attempts++;
        }
        while (attempts < 24 &&
               (Math.Abs(targetX - startX) + Math.Abs(targetY - startY) < 150d ||
                Distance(baseCenterX + targetX, baseCenterY + targetY, pointerPosition.X, pointerPosition.Y) < 170d));

        transform.X = targetX;
        transform.Y = targetY;
        PaywallCloseButton.RenderTransform = transform;
    }

    private static double Distance(double firstX, double firstY, double secondX, double secondY)
    {
        var deltaX = firstX - secondX;
        var deltaY = firstY - secondY;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private void OnPurchaseCompleted(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => _ = PlayPurchaseCelebrationAsync());

    private async Task PlayPurchaseCelebrationAsync()
    {
        CelebrationLayer.IsVisible = true;
        PurchaseToast.Opacity = 1;

        if (Motion.Reduced)
        {
            await Task.Delay(1600);
            CelebrationLayer.IsVisible = false;
            return;
        }

        ConfettiCanvas.Children.Clear();
        var random = new Random(20261115);
        var colors = new[] { "#FFFFF36D", "#FF54F3FF", "#FFFF5A8F", "#FFF49A58", "#FF8B7CFF" };
        var width = Math.Max(Bounds.Width, 390d);
        var height = Math.Max(Bounds.Height, 650d);
        var animations = new List<Task>();

        PurchaseToast.RenderTransform = new TranslateTransform();
        var toastAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(520),
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
                        new Setter(TranslateTransform.YProperty, -18d),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(0.5d),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1d),
                        new Setter(TranslateTransform.YProperty, 5d),
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
        animations.Add(toastAnimation.RunAsync(PurchaseToast));

        for (var index = 0; index < 48; index++)
        {
            var piece = new Border
            {
                Width = random.Next(8, 14),
                Height = random.Next(12, 24),
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.Parse(colors[index % colors.Length])),
                RenderTransform = new TranslateTransform(),
            };

            Canvas.SetLeft(piece, random.NextDouble() * Math.Max(1d, width - 12d));
            Canvas.SetTop(piece, -20d - random.NextDouble() * 260d);
            ConfettiCanvas.Children.Add(piece);

            var drift = random.NextDouble() * 70d - 35d;
            var fall = height + 240d + random.NextDouble() * 140d;
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(1250 + random.Next(0, 650)),
                Easing = new CubicEaseIn(),
                FillMode = FillMode.None,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(OpacityProperty, 1d),
                            new Setter(TranslateTransform.XProperty, 0d),
                            new Setter(TranslateTransform.YProperty, 0d),
                        },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(0.78d),
                        Setters =
                        {
                            new Setter(OpacityProperty, 1d),
                            new Setter(TranslateTransform.XProperty, drift),
                            new Setter(TranslateTransform.YProperty, fall * 0.78d),
                        },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(OpacityProperty, 0d),
                            new Setter(TranslateTransform.XProperty, drift * 1.25d),
                            new Setter(TranslateTransform.YProperty, fall),
                        },
                    },
                },
            };

            animations.Add(animation.RunAsync(piece));
        }

        await Task.WhenAll(animations);
        ConfettiCanvas.Children.Clear();
        CelebrationLayer.IsVisible = false;
    }

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

    private void AnimatePaywallNag()
    {
        if (Motion.Reduced)
        {
            PaywallCloseButton.Focus();
            return;
        }

        PaywallSheet.RenderTransform = new TranslateTransform();
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(240),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                KeyFrame(0, 0, 1),
                KeyFrame(0.18, -8, 1),
                KeyFrame(0.42, 7, 1),
                KeyFrame(0.68, -4, 1),
                KeyFrame(1, 0, 1),
            },
        };

        _ = animation.RunAsync(PaywallSheet);
        PaywallCloseButton.Focus();
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
