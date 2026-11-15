using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SumScription.Core.Calculator;
using SumScription.Core.Paywall;

namespace SumScription.App.ViewModels;

/// <summary>Dünne Steuerung über <see cref="CalculatorSession"/>: Anzeige, Overlays, Kommandos.</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const int FadedHistoryRows = 5;

    private readonly CalculatorSession _session;
    private readonly ObservableCollection<HistoryRow> _history = [];

    private bool _paywallShownThisRun;
    private bool _isPaywallVisible;
    private bool _isCheckoutVisible;
    private int _paywallDismissalStep;
    private PaywallPlan _selectedPlan = PaywallPlan.Subscription;
    private string _expressionText = string.Empty;
    private string _displayResult = "0";
    private bool _hasError;
    private bool _isResultMasked;

    /// <summary>Erzeugt das ViewModel für eine Sitzung.</summary>
    public MainWindowViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;

        DigitCommand = new RelayCommand(parameter => RunDigit(AsText(parameter)));
        SeparatorCommand = new RelayCommand(_ => Run(Separator));
        OperatorCommand = new RelayCommand(parameter => RunOperator(AsText(parameter)));
        EqualsCommand = new RelayCommand(_ => Evaluate());
        ToggleSignCommand = new RelayCommand(_ => Run(_session.ToggleSign));
        PercentCommand = new RelayCommand(_ => Run(_session.Percent));
        BackspaceCommand = new RelayCommand(_ => Run(_session.Backspace));
        ClearEntryCommand = new RelayCommand(_ => Run(_session.ClearEntry));
        ClearAllCommand = new RelayCommand(_ => Run(_session.ClearAll));
        OpenPaywallCommand = new RelayCommand(_ => OpenPaywall());
        ClosePaywallCommand = new RelayCommand(_ => ClosePaywall());
        SelectPlanCommand = new RelayCommand(parameter => SelectPlan(AsText(parameter)));
        ConfirmPurchaseCommand = new RelayCommand(_ => ConfirmPurchase());
        CancelPurchaseCommand = new RelayCommand(_ => CancelCheckout());

        Refresh();
    }

    /// <summary>Ein Ergebnis ist erschienen.</summary>
    public event EventHandler? Evaluated;

    /// <summary>Ein Fehler ist erschienen.</summary>
    public event EventHandler? ErrorAppeared;

    /// <summary>Das Preisblatt wurde geöffnet.</summary>
    public event EventHandler? PaywallOpened;

    /// <summary>Die Parodie-Paywall hat die nächste Ablehnungsstufe erreicht.</summary>
    public event EventHandler? PaywallDismissalAdvanced;

    /// <summary>Der Kaufdialog wurde geöffnet.</summary>
    public event EventHandler? CheckoutOpened;

    /// <summary>Alle Overlays sind geschlossen.</summary>
    public event EventHandler? OverlaysClosed;

    /// <summary>Der Kaufdialog wurde geschlossen.</summary>
    public event EventHandler? CheckoutClosed;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Ziffernkommando, Parameter ist die Ziffer.</summary>
    public RelayCommand DigitCommand { get; }

    /// <summary>Dezimaltrennzeichen.</summary>
    public RelayCommand SeparatorCommand { get; }

    /// <summary>Operatorkommando (+, -, *, / oder Anzeigezeichen).</summary>
    public RelayCommand OperatorCommand { get; }

    /// <summary>Gleichheitszeichen.</summary>
    public RelayCommand EqualsCommand { get; }

    /// <summary>Vorzeichenwechsel.</summary>
    public RelayCommand ToggleSignCommand { get; }

    /// <summary>Prozent.</summary>
    public RelayCommand PercentCommand { get; }

    /// <summary>Rücktaste.</summary>
    public RelayCommand BackspaceCommand { get; }

    /// <summary>Eintrag löschen.</summary>
    public RelayCommand ClearEntryCommand { get; }

    /// <summary>Alles löschen.</summary>
    public RelayCommand ClearAllCommand { get; }

    /// <summary>Preisblatt öffnen.</summary>
    public RelayCommand OpenPaywallCommand { get; }

    /// <summary>Preisblatt schließen.</summary>
    public RelayCommand ClosePaywallCommand { get; }

    /// <summary>Tarif wählen, Parameter ist <see cref="PaywallPlan"/> als Text.</summary>
    public RelayCommand SelectPlanCommand { get; }

    /// <summary>Demo-Kauf bestätigen.</summary>
    public RelayCommand ConfirmPurchaseCommand { get; }

    /// <summary>Kauf abbrechen.</summary>
    public RelayCommand CancelPurchaseCommand { get; }

    /// <summary>Der sichtbare Ausdruck.</summary>
    public string ExpressionText
    {
        get => _expressionText;
        private set => SetField(ref _expressionText, value);
    }

    /// <summary>Der sichtbare Eintrag bzw. das Ergebnis.</summary>
    public string DisplayResult
    {
        get => _displayResult;
        private set => SetField(ref _displayResult, value);
    }

    /// <summary>Wahr, wenn ein Fehler angezeigt wird.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetField(ref _hasError, value);
    }

    /// <summary>Wahr, wenn das Ergebnis verdeckt ist.</summary>
    public bool IsResultMasked
    {
        get => _isResultMasked;
        private set => SetField(ref _isResultMasked, value);
    }

    /// <summary>Der zuletzt gewählte Tarif.</summary>
    public PaywallPlan SelectedPlan => _selectedPlan;

    /// <summary>Wahr, wenn die Demo freigeschaltet ist.</summary>
    public bool IsUnlocked => _session.Paywall.State.IsUnlocked;

    /// <summary>Wahr, wenn die automatisch gezeigte Paywall danach erneut geöffnet werden darf.</summary>
    public bool CanReopenPaywall => _paywallShownThisRun && !IsUnlocked;

    /// <summary>Barrierefreier Name der Ergebniszeile; maskiert gibt er den Wert nicht preis.</summary>
    public string ResultAutomationName => IsResultMasked ? "Ergebnis verdeckt" : "Ergebnis";

    /// <summary>Wahr, wenn das Preisblatt sichtbar ist.</summary>
    public bool IsPaywallVisible
    {
        get => _isPaywallVisible;
        private set
        {
            if (SetField(ref _isPaywallVisible, value))
            {
                RaisePropertyChanged(nameof(IsOverlayOpen));
            }
        }
    }

    /// <summary>Wahr, wenn der Kaufdialog sichtbar ist.</summary>
    public bool IsCheckoutVisible
    {
        get => _isCheckoutVisible;
        private set
        {
            if (SetField(ref _isCheckoutVisible, value))
            {
                RaisePropertyChanged(nameof(IsOverlayOpen));
            }
        }
    }

    /// <summary>Wahr, wenn ein Overlay die Tastatur abfängt.</summary>
    public bool IsOverlayOpen => IsPaywallVisible || IsCheckoutVisible;

    /// <summary>Überschrift der aktuellen, absichtlich übertriebenen Ablehnungsstufe.</summary>
    public string PaywallTitle => _paywallDismissalStep switch
    {
        1 => "Moment. Wirklich sicher?",
        2 => "Wir fragen ein letztes Mal.*",
        _ => "Dein Ergebnis ist fertig.",
    };

    /// <summary>Begleittext der aktuellen Ablehnungsstufe.</summary>
    public string PaywallMessage => _paywallDismissalStep switch
    {
        1 => "Ohne Pro bleibt dieses Ergebnis leider emotional verschlossen.",
        2 => "*Das war gelogen. Danach darfst du aber wirklich weiterrechnen.",
        _ => "Aber offenbar gehört die Antwort jetzt unserem Premium-Team.",
    };

    /// <summary>Text des Ablehnen-Knopfs.</summary>
    public string PaywallCloseText => _paywallDismissalStep switch
    {
        1 => "Ja. Wirklich ablehnen",
        2 => "Letzte Chance ignorieren",
        _ => "Nein danke",
    };

    /// <summary>Transparenter Hinweis darauf, wie oft die Parodie noch abgelehnt werden muss.</summary>
    public string PaywallDismissalHint
    {
        get
        {
            var remaining = 3 - _paywallDismissalStep;
            return remaining == 1
                ? "Noch 1 Ablehnung bis zurück zum Rechner"
                : $"Noch {remaining} Ablehnungen bis zurück zum Rechner";
        }
    }

    /// <summary>Die Zeilen des Rechenbands, neueste zuerst.</summary>
    public ObservableCollection<HistoryRow> History => _history;

    /// <summary>Wahr, wenn der Verlauf leer ist.</summary>
    public bool IsHistoryEmpty => _history.Count == 0;

    /// <summary>Tarifzeile im Kaufdialog.</summary>
    public string CheckoutPlanLine => _selectedPlan == PaywallPlan.OneTime
        ? "Lebenslang rechnen · fiktive 499,99 €"
        : "Ultra-Abo · fiktive 29,99 € pro Woche";

    /// <summary>Zweite Zusammenfassungszeile im Kaufdialog.</summary>
    public string CheckoutNoteLine => _selectedPlan == PaywallPlan.OneTime
        ? "Einmal erfunden, dauerhaft freigeschaltet"
        : "Jederzeit fiktiv kündbar";

    /// <summary>Verarbeitet eine Ziffer aus der Tastatur.</summary>
    public void TypeDigit(char digit)
    {
        if (IsOverlayOpen || !char.IsAsciiDigit(digit))
        {
            return;
        }

        _session.InputDigit(digit);
        Refresh();
    }

    /// <summary>Verarbeitet das Dezimaltrennzeichen.</summary>
    public void TypeSeparator()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        Separator();
    }

    /// <summary>Verarbeitet einen Operator aus der Tastatur.</summary>
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

    /// <summary>Wertet aus.</summary>
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
            OpenPaywall();
        }
    }

    /// <summary>Wechselt das Vorzeichen.</summary>
    public void ToggleSign()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.ToggleSign();
        Refresh();
    }

    /// <summary>Teilt den Eintrag durch 100.</summary>
    public void Percent()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.Percent();
        Refresh();
    }

    /// <summary>Löscht die letzte Ziffer.</summary>
    public void Backspace()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.Backspace();
        Refresh();
    }

    /// <summary>Setzt den Eintrag auf 0.</summary>
    public void ClearEntry()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.ClearEntry();
        Refresh();
    }

    /// <summary>Löscht alles.</summary>
    public void ClearAll()
    {
        if (IsOverlayOpen)
        {
            return;
        }

        _session.ClearAll();
        Refresh();
    }

    /// <summary>Öffnet das Preisblatt.</summary>
    public void OpenPaywall()
    {
        if (IsUnlocked)
        {
            return;
        }

        if (IsPaywallVisible)
        {
            if (IsCheckoutVisible)
            {
                IsCheckoutVisible = false;
            }

            return;
        }

        IsCheckoutVisible = false;
        _paywallDismissalStep = 0;
        RaisePaywallCopyChanged();
        IsPaywallVisible = true;
        PaywallOpened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Schließt das Preisblatt.</summary>
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
        _paywallDismissalStep = 0;
        RaisePaywallCopyChanged();
        OverlaysClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Schließt den Kaufdialog ohne Kauf.</summary>
    public void CancelCheckout()
    {
        if (!IsCheckoutVisible)
        {
            return;
        }

        IsCheckoutVisible = false;
        CheckoutClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Wählt einen Tarif und öffnet den Kaufdialog.</summary>
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

    /// <summary>Bestätigt den Demo-Kauf.</summary>
    public void ConfirmPurchase()
    {
        if (!IsCheckoutVisible)
        {
            return;
        }

        _session.Unlock(_selectedPlan);
        IsCheckoutVisible = false;
        IsPaywallVisible = false;
        _paywallDismissalStep = 0;
        RaisePaywallCopyChanged();
        Refresh();
        OverlaysClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Setzt die Demo zurück.</summary>
    public void ResetDemo()
    {
        _session.ResetDemo();
        _paywallShownThisRun = false;
        RaisePropertyChanged(nameof(CanReopenPaywall));
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

    private void Refresh()
    {
        ExpressionText = _session.ExpressionText;
        HasError = _session.HasError;
        IsResultMasked = _session.IsResultMasked;
        RaisePropertyChanged(nameof(ResultAutomationName));
        DisplayResult = _session.DisplayResult;
        RaisePropertyChanged(nameof(IsUnlocked));
        RaisePropertyChanged(nameof(CanReopenPaywall));
        RebuildHistory();
    }

    private void RaisePaywallCopyChanged()
    {
        RaisePropertyChanged(nameof(PaywallTitle));
        RaisePropertyChanged(nameof(PaywallMessage));
        RaisePropertyChanged(nameof(PaywallCloseText));
        RaisePropertyChanged(nameof(PaywallDismissalHint));
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
}
