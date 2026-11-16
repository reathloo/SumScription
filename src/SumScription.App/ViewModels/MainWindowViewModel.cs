using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.App.ViewModels;

/// <summary>Coordinates calculator display, overlays, commands, and parody purchases.</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const int FadedHistoryRows = 5;

    private readonly CalculatorSession _session;
    private readonly ObservableCollection<HistoryRow> _history = [];

    private bool _paywallShownThisRun;
    private bool _isPaywallVisible;
    private bool _isCheckoutVisible;
    private int _paywallDismissalStep;
    private PaywallReason _paywallReason;
    private bool _pendingPercent;
    private bool _showAnnualPlans;
    private PaywallPlan _selectedPlan = PaywallPlan.Monthly;
    private PremiumPercentPack _selectedPremiumPercentPack = PremiumPercentPack.Ten;
    private string _expressionText = string.Empty;
    private string _displayResult = "0";
    private string _purchaseCelebrationText = "PREMIUM ARITHMETIC ACQUIRED ✨";
    private bool _hasError;
    private bool _isResultMasked;

    /// <summary>Creates the view model for a calculator session.</summary>
    public MainWindowViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;

        DigitCommand = new RelayCommand(parameter => RunDigit(AsText(parameter)));
        SeparatorCommand = new RelayCommand(_ => Run(Separator));
        OperatorCommand = new RelayCommand(parameter => RunOperator(AsText(parameter)));
        EqualsCommand = new RelayCommand(_ => Evaluate());
        ToggleSignCommand = new RelayCommand(_ => Run(_session.ToggleSign));
        PercentCommand = new RelayCommand(_ => Percent());
        BackspaceCommand = new RelayCommand(_ => Run(_session.Backspace));
        ClearEntryCommand = new RelayCommand(_ => Run(_session.ClearEntry));
        ClearAllCommand = new RelayCommand(_ => Run(_session.ClearAll));
        OpenPaywallCommand = new RelayCommand(_ => OpenPaywall());
        SelectPlanCommand = new RelayCommand(parameter => SelectPlan(AsText(parameter)));
        ShowMonthlyPlansCommand = new RelayCommand(_ => ShowAnnualPlans(false));
        ShowAnnualPlansCommand = new RelayCommand(_ => ShowAnnualPlans(true));
        SelectPremiumPercentPackCommand = new RelayCommand(parameter => SelectPremiumPercentPack(AsText(parameter)));
        ConfirmPurchaseCommand = new RelayCommand(_ => ConfirmPurchase());
        CancelPurchaseCommand = new RelayCommand(_ => CancelCheckout());

        Refresh();
    }

    /// <summary>A result appeared.</summary>
    public event EventHandler? Evaluated;

    /// <summary>An error appeared.</summary>
    public event EventHandler? ErrorAppeared;

    /// <summary>The parody paywall opened.</summary>
    public event EventHandler? PaywallOpened;

    /// <summary>The parody paywall advanced to its next refusal stage.</summary>
    public event EventHandler? PaywallDismissalAdvanced;

    /// <summary>The fictional checkout opened.</summary>
    public event EventHandler? CheckoutOpened;

    /// <summary>All overlays closed.</summary>
    public event EventHandler? OverlaysClosed;

    /// <summary>The fictional checkout closed.</summary>
    public event EventHandler? CheckoutClosed;

    /// <summary>A fictional purchase completed and should be celebrated.</summary>
    public event EventHandler? PurchaseCompleted;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Digit command whose parameter is the digit.</summary>
    public RelayCommand DigitCommand { get; }

    /// <summary>Decimal-separator command.</summary>
    public RelayCommand SeparatorCommand { get; }

    /// <summary>Operator command for +, -, *, /, or display symbols.</summary>
    public RelayCommand OperatorCommand { get; }

    /// <summary>Equals command.</summary>
    public RelayCommand EqualsCommand { get; }

    /// <summary>Sign-toggle command.</summary>
    public RelayCommand ToggleSignCommand { get; }

    /// <summary>Premium percentage operation.</summary>
    public RelayCommand PercentCommand { get; }

    /// <summary>Backspace command.</summary>
    public RelayCommand BackspaceCommand { get; }

    /// <summary>Clear-entry command.</summary>
    public RelayCommand ClearEntryCommand { get; }

    /// <summary>Clear-all command.</summary>
    public RelayCommand ClearAllCommand { get; }

    /// <summary>Opens the result paywall.</summary>
    public RelayCommand OpenPaywallCommand { get; }

    /// <summary>Selects a result-unlock plan from its enum name.</summary>
    public RelayCommand SelectPlanCommand { get; }

    /// <summary>Shows the monthly result offers.</summary>
    public RelayCommand ShowMonthlyPlansCommand { get; }

    /// <summary>Shows the annual result offers.</summary>
    public RelayCommand ShowAnnualPlansCommand { get; }

    /// <summary>Selects a premium percentage pack from its enum name.</summary>
    public RelayCommand SelectPremiumPercentPackCommand { get; }

    /// <summary>Confirms a fictional purchase.</summary>
    public RelayCommand ConfirmPurchaseCommand { get; }

    /// <summary>Cancels fictional checkout.</summary>
    public RelayCommand CancelPurchaseCommand { get; }

    /// <summary>The visible expression.</summary>
    public string ExpressionText
    {
        get => _expressionText;
        private set => SetField(ref _expressionText, value);
    }

    /// <summary>The visible entry or result.</summary>
    public string DisplayResult
    {
        get => _displayResult;
        private set => SetField(ref _displayResult, value);
    }

    /// <summary>Whether an error is visible.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetField(ref _hasError, value);
    }

    /// <summary>Whether the result is masked.</summary>
    public bool IsResultMasked
    {
        get => _isResultMasked;
        private set => SetField(ref _isResultMasked, value);
    }

    /// <summary>The selected result-unlock plan.</summary>
    public PaywallPlan SelectedPlan => _selectedPlan;

    /// <summary>Whether the monthly result offers are active.</summary>
    public bool IsMonthlyPlans => !_showAnnualPlans;

    /// <summary>Whether the annual result offers are active.</summary>
    public bool IsAnnualPlans => _showAnnualPlans;

    /// <summary>Opacity for the preloaded monthly offer group.</summary>
    public double MonthlyPlansOpacity => IsMonthlyPlans ? 1d : 0d;

    /// <summary>Opacity for the preloaded annual offer group.</summary>
    public double AnnualPlansOpacity => IsAnnualPlans ? 1d : 0d;

    /// <summary>The selected premium percentage pack.</summary>
    public PremiumPercentPack SelectedPremiumPercentPack => _selectedPremiumPercentPack;

    /// <summary>Whether the result paywall is fictionally unlocked.</summary>
    public bool IsUnlocked => _session.Paywall.State.IsUnlocked;

    /// <summary>Unused premium percentage calculations.</summary>
    public int PremiumPercentCredits => _session.Paywall.State.PremiumPercentCredits;

    /// <summary>Confirmation shown after a fictional purchase.</summary>
    public string PurchaseCelebrationText
    {
        get => _purchaseCelebrationText;
        private set => SetField(ref _purchaseCelebrationText, value);
    }

    /// <summary>Whether the result paywall can be reopened after its first appearance.</summary>
    public bool CanReopenPaywall => _paywallShownThisRun && !IsUnlocked;

    /// <summary>Whether the edge shop icon can be shown without competing with an overlay.</summary>
    public bool IsShopIconVisible => CanReopenPaywall && !IsOverlayOpen;

    /// <summary>Accessible result name that never leaks a masked value.</summary>
    public string ResultAutomationName => IsResultMasked ? "Result hidden" : "Result";

    /// <summary>Whether the current overlay sells percentage credits.</summary>
    public bool IsPremiumPercentPaywall => _paywallReason == PaywallReason.PremiumPercent;

    /// <summary>Whether the current overlay sells the result unlock.</summary>
    public bool IsResultPaywall => !IsPremiumPercentPaywall;

    /// <summary>Opacity for the preloaded premium percentage offers.</summary>
    public double PremiumPercentOfferOpacity => IsPremiumPercentPaywall ? 1d : 0d;

    /// <summary>Opacity for the preloaded result offers.</summary>
    public double ResultOfferOpacity => IsResultPaywall ? 1d : 0d;

    /// <summary>Whether the paywall is visible.</summary>
    public bool IsPaywallVisible
    {
        get => _isPaywallVisible;
        private set
        {
            if (SetField(ref _isPaywallVisible, value))
            {
                RaisePropertyChanged(nameof(PaywallOpacity));
                RaisePropertyChanged(nameof(IsOverlayOpen));
                RaisePropertyChanged(nameof(IsShopIconVisible));
            }
        }
    }

    /// <summary>Opacity used to keep the paywall preloaded without accepting input.</summary>
    public double PaywallOpacity => IsPaywallVisible ? 1d : 0d;

    /// <summary>Whether fictional checkout is visible.</summary>
    public bool IsCheckoutVisible
    {
        get => _isCheckoutVisible;
        private set
        {
            if (SetField(ref _isCheckoutVisible, value))
            {
                RaisePropertyChanged(nameof(IsOverlayOpen));
                RaisePropertyChanged(nameof(IsShopIconVisible));
            }
        }
    }

    /// <summary>Whether an overlay currently captures keyboard input.</summary>
    public bool IsOverlayOpen => IsPaywallVisible || IsCheckoutVisible;

    /// <summary>Banner copy for the current parody offer.</summary>
    public string PaywallBanner => IsPremiumPercentPaywall
        ? "⚡ BREAKING: THE % SIGN HAS A PRICE ⚡"
        : "⚡ TODAY ONLY: THE WORLD'S MOST EXPENSIVE ANSWER ⚡";

    /// <summary>Headline for the current refusal stage.</summary>
    public string PaywallTitle => (IsPremiumPercentPaywall, _paywallDismissalStep) switch
    {
        (true, 1) => "Wait. You would divide by 100 for free?",
        (true, 2) => "Final offer. Probably.*",
        (true, _) => "Percent is a premium feature.",
        (false, 1) => "Wait. Are you financially sure?",
        (false, 2) => "We will ask one final time.*",
        _ => "Your answer is ready.",
    };

    /// <summary>Supporting copy for the current refusal stage.</summary>
    public string PaywallMessage => (IsPremiumPercentPaywall, _paywallDismissalStep) switch
    {
        (true, 1) => "That tiny symbol represents decades of imaginary research and development.",
        (true, 2) => "*That was a lie. You may return to ordinary arithmetic after this.",
        (true, _) => "Advanced mathematics deserves an advanced invoice.",
        (false, 1) => "Without Pro, this result remains emotionally unavailable.",
        (false, 2) => "*That was a lie. You may actually continue after this.",
        _ => "Unfortunately, the answer now belongs to our Premium Results Department.",
    };

    /// <summary>Alternating text for the refusal button.</summary>
    public string PaywallCloseText => (IsPremiumPercentPaywall, _paywallDismissalStep) switch
    {
        (true, 1) => "I reject premium division",
        (true, 2) => "Keep my percentage locked",
        (true, _) => "No, I can divide manually",
        (false, 1) => "Yes, reject this incredible value",
        (false, 2) => "Ignore the final final offer",
        _ => "No thanks, I dislike answers",
    };

    /// <summary>History rows, newest first.</summary>
    public ObservableCollection<HistoryRow> History => _history;

    /// <summary>Whether history is empty.</summary>
    public bool IsHistoryEmpty => _history.Count == 0;

    /// <summary>Selected fictional checkout offer.</summary>
    public string CheckoutPlanLine => IsPremiumPercentPaywall
        ? _selectedPremiumPercentPack switch
        {
            PremiumPercentPack.One => "1 percentage calculation · €9.99",
            PremiumPercentPack.Ten => "10 percentage calculations · €69.99",
            PremiumPercentPack.Hundred => "100 percentage calculations · €599.00",
            _ => throw new ArgumentOutOfRangeException(),
        }
        : _selectedPlan switch
        {
            PaywallPlan.SingleCalculation => "1 result · €4.99",
            PaywallPlan.AnnualPremium => "Annual Premium · €699 · includes 100 % calculations",
            PaywallPlan.Annual => "Annual Standard · €499 per year",
            _ => "Monthly Ultra · €74.99 per month",
        };

    /// <summary>Secondary fictional checkout summary.</summary>
    public string CheckoutNoteLine => IsPremiumPercentPaywall
        ? _selectedPremiumPercentPack switch
        {
            PremiumPercentPack.One => "One premium calculation. No rollover. Maximum prestige.",
            PremiumPercentPack.Ten => "Save an imaginary 30%. Our accountants are devastated.",
            PremiumPercentPack.Hundred => "For percentage power users with premium expense accounts.",
            _ => throw new ArgumentOutOfRangeException(),
        }
        : _selectedPlan switch
        {
            PaywallPlan.SingleCalculation => "Special offer · valid for this result only",
            PaywallPlan.AnnualPremium => "One year of results plus 100 premium percentage credits",
            PaywallPlan.Annual => "One year of premium results without percentage credits",
            _ => "Renews monthly · cancellable at any time",
        };

    /// <summary>Handles a digit from keyboard input.</summary>
    public void TypeDigit(char digit)
    {
        if (IsOverlayOpen || !char.IsAsciiDigit(digit))
        {
            return;
        }

        _session.InputDigit(digit);
        Refresh();
    }

    /// <summary>Handles the decimal separator.</summary>
    public void TypeSeparator()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        Separator();
    }

    /// <summary>Handles an operator from keyboard input.</summary>
    public void TypeOperator(char symbol)
    {
        if (IsOverlayOpen)
        {
            return;
        }

        if (_session.InputOperator(symbol))
        {
            Refresh();
        }
    }

    /// <summary>Evaluates the current expression.</summary>
    public void Evaluate()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        var evaluated = _session.Evaluate();
        Refresh();

        if (evaluated)
        {
            Evaluated?.Invoke(this, EventArgs.Empty);
        }
        else if (_session.HasError)
        {
            ErrorAppeared?.Invoke(this, EventArgs.Empty);
        }

        if (evaluated && _session.IsResultMasked && !_paywallShownThisRun)
        {
            _paywallShownThisRun = true;
            RaisePropertyChanged(nameof(CanReopenPaywall));
            RaisePropertyChanged(nameof(IsShopIconVisible));
            OpenPaywall();
        }
    }

    /// <summary>Toggles the sign.</summary>
    public void ToggleSign()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.ToggleSign();
        Refresh();
    }

    /// <summary>Uses a premium credit or opens the percentage paywall.</summary>
    public void Percent()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        if (_session.TryPercent())
        {
            Refresh();
            return;
        }

        _pendingPercent = true;
        OpenPaywall(PaywallReason.PremiumPercent);
    }

    /// <summary>Deletes the last digit.</summary>
    public void Backspace()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.Backspace();
        Refresh();
    }

    /// <summary>Resets the current entry to zero.</summary>
    public void ClearEntry()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.ClearEntry();
        Refresh();
    }

    /// <summary>Clears the calculator.</summary>
    public void ClearAll()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.ClearAll();
        Refresh();
    }

    /// <summary>Opens the result-unlock paywall.</summary>
    public void OpenPaywall()
    {
        if (IsUnlocked)
        {
            return;
        }

        OpenPaywall(PaywallReason.Result);
    }

    private void OpenPaywall(PaywallReason reason)
    {
        if (IsPaywallVisible)
        {
            if (IsCheckoutVisible)
            {
                IsCheckoutVisible = false;
            }

            return;
        }

        IsCheckoutVisible = false;
        _paywallReason = reason;
        _paywallDismissalStep = 0;
        RaisePaywallCopyChanged();
        RaisePropertyChanged(nameof(IsPremiumPercentPaywall));
        RaisePropertyChanged(nameof(IsResultPaywall));
        RaisePropertyChanged(nameof(PremiumPercentOfferOpacity));
        RaisePropertyChanged(nameof(ResultOfferOpacity));
        RaisePropertyChanged(nameof(CheckoutPlanLine));
        RaisePropertyChanged(nameof(CheckoutNoteLine));
        IsPaywallVisible = true;
        PaywallOpened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Advances the refusal flow or closes the paywall.</summary>
    public void ClosePaywall()
    {
        if (!IsPaywallVisible)
        {
            return;
        }

        if (_paywallDismissalStep < 2)
        {
            _paywallDismissalStep++;
            RaisePaywallCopyChanged();
            PaywallDismissalAdvanced?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsCheckoutVisible = false;
        IsPaywallVisible = false;
        _pendingPercent = false;
        _paywallDismissalStep = 0;
        RaisePaywallCopyChanged();
        OverlaysClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Closes checkout without a purchase.</summary>
    public void CancelCheckout()
    {
        if (!IsCheckoutVisible)
        {
            return;
        }

        IsCheckoutVisible = false;
        CheckoutClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Selects a result plan and opens fictional checkout.</summary>
    public void SelectPlan(PaywallPlan plan)
    {
        if (plan == PaywallPlan.None)
        {
            return;
        }

        _selectedPlan = plan;
        RaisePropertyChanged(nameof(SelectedPlan));
        RaisePropertyChanged(nameof(CheckoutPlanLine));
        RaisePropertyChanged(nameof(CheckoutNoteLine));
        IsCheckoutVisible = true;
        CheckoutOpened?.Invoke(this, EventArgs.Empty);
    }

    private void ShowAnnualPlans(bool showAnnual)
    {
        if (_showAnnualPlans == showAnnual)
        {
            return;
        }

        _showAnnualPlans = showAnnual;
        RaisePropertyChanged(nameof(IsMonthlyPlans));
        RaisePropertyChanged(nameof(IsAnnualPlans));
        RaisePropertyChanged(nameof(MonthlyPlansOpacity));
        RaisePropertyChanged(nameof(AnnualPlansOpacity));
    }

    /// <summary>Selects a premium percentage pack and opens fictional checkout.</summary>
    public void SelectPremiumPercentPack(PremiumPercentPack pack)
    {
        if (!Enum.IsDefined(pack))
        {
            return;
        }

        _selectedPremiumPercentPack = pack;
        RaisePropertyChanged(nameof(SelectedPremiumPercentPack));
        RaisePropertyChanged(nameof(CheckoutPlanLine));
        RaisePropertyChanged(nameof(CheckoutNoteLine));
        IsCheckoutVisible = true;
        CheckoutOpened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Confirms the fictional purchase.</summary>
    public void ConfirmPurchase()
    {
        if (!IsCheckoutVisible)
        {
            return;
        }

        if (IsPremiumPercentPaywall)
        {
            _session.Paywall.PurchasePremiumPercentPack(_selectedPremiumPercentPack);
            if (_pendingPercent)
            {
                _session.TryPercent();
            }

            PurchaseCelebrationText = _session.Paywall.State.PremiumPercentCredits == 1
                ? "PREMIUM % UNLOCKED · 1 CREDIT LEFT ✨"
                : $"PREMIUM % UNLOCKED · {_session.Paywall.State.PremiumPercentCredits} CREDITS LEFT ✨";
        }
        else
        {
            _session.Unlock(_selectedPlan);
            if (_selectedPlan == PaywallPlan.AnnualPremium)
            {
                _session.Paywall.PurchasePremiumPercentPack(PremiumPercentPack.Hundred);
            }

            PurchaseCelebrationText = _selectedPlan switch
            {
                PaywallPlan.SingleCalculation => "ONE EXTREMELY PREMIUM RESULT ACQUIRED ✨",
                PaywallPlan.AnnualPremium => "ANNUAL PREMIUM + 100 % CREDITS ACQUIRED ✨",
                _ => "PREMIUM ARITHMETIC ACQUIRED ✨",
            };
        }

        _pendingPercent = false;
        IsCheckoutVisible = false;
        IsPaywallVisible = false;
        _paywallDismissalStep = 0;
        RaisePaywallCopyChanged();
        Refresh();
        PurchaseCompleted?.Invoke(this, EventArgs.Empty);
        OverlaysClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Resets all local parody state.</summary>
    public void ResetDemo()
    {
        _session.ResetDemo();
        _paywallShownThisRun = false;
        _pendingPercent = false;
        RaisePropertyChanged(nameof(CanReopenPaywall));
        RaisePropertyChanged(nameof(IsShopIconVisible));
        IsCheckoutVisible = false;
        IsPaywallVisible = false;
        _paywallDismissalStep = 0;
        RaisePaywallCopyChanged();
        Refresh();
        OverlaysClosed?.Invoke(this, EventArgs.Empty);
    }

    private static string AsText(object? parameter) => parameter switch
    {
        null => string.Empty,
        char character => character.ToString(),
        string text => text,
        PaywallPlan plan => plan.ToString(),
        _ => parameter.ToString() ?? string.Empty,
    };

    private void RunDigit(string text)
    {
        if (IsOverlayOpen || text.Length == 0)
        {
            return;
        }

        TypeDigit(text[0]);
    }

    private void Separator()
    {
        _session.InputDecimalSeparator();
        Refresh();
    }

    private void RunOperator(string text)
    {
        if (IsOverlayOpen || text.Length == 0)
        {
            return;
        }

        TypeOperator(text[0]);
    }

    private void Run(Action action)
    {
        if (IsOverlayOpen)
        {
            return;
        }

        action();
        Refresh();
    }

    private void SelectPlan(string text)
    {
        if (!Enum.TryParse<PaywallPlan>(text, ignoreCase: true, out var plan))
        {
            return;
        }

        SelectPlan(plan);
    }

    private void SelectPremiumPercentPack(string text)
    {
        if (!Enum.TryParse<PremiumPercentPack>(text, ignoreCase: true, out var pack))
        {
            return;
        }

        SelectPremiumPercentPack(pack);
    }

    private void Refresh()
    {
        ExpressionText = _session.ExpressionText;
        HasError = _session.HasError;
        IsResultMasked = _session.IsResultMasked;
        RaisePropertyChanged(nameof(ResultAutomationName));
        DisplayResult = _session.DisplayResult;
        RaisePropertyChanged(nameof(IsUnlocked));
        RaisePropertyChanged(nameof(PremiumPercentCredits));
        RaisePropertyChanged(nameof(CanReopenPaywall));
        RaisePropertyChanged(nameof(IsShopIconVisible));
        RebuildHistory();
    }

    private void RaisePaywallCopyChanged()
    {
        RaisePropertyChanged(nameof(PaywallTitle));
        RaisePropertyChanged(nameof(PaywallBanner));
        RaisePropertyChanged(nameof(PaywallMessage));
        RaisePropertyChanged(nameof(PaywallCloseText));
    }

    private void RebuildHistory()
    {
        _history.Clear();
        var entries = _session.History;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var faded = i >= entries.Count - FadedHistoryRows;
            _history.Add(new HistoryRow(entry.Expression, entry.Value, entry.IsMasked, faded ? 0.45 : 1.0));
        }

        RaisePropertyChanged(nameof(IsHistoryEmpty));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    private void RaisePropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private enum PaywallReason
    {
        Result,
        PremiumPercent,
    }
}
