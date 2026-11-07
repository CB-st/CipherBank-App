// <copyright file="SendViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Send / transfer with full ACH recipient form.</summary>
public partial class SendViewModel : ObservableObject
{
    private readonly IProductClient _api;
    private readonly IDialogService _dialogs;
    private readonly IAppSession _session;
    private readonly IStepUpAuth _stepUp;
    private readonly IRecipientRepository _recipients;
    private readonly IRecipientSeedInitializer _recipientInitializer;
    private readonly IPrefsStore _prefs;

    private readonly TimeProvider _timeProvider;

    public SendViewModel(
        IProductClient api,
        IDialogService dialogs,
        IAppSession session,
        IStepUpAuth stepUp,
        IRecipientRepository recipients,
        IRecipientSeedInitializer recipientInitializer,
        IPrefsStore prefs,
        TimeProvider timeProvider,
        ICoraLineProvider coraLines)
    {
        _timeProvider = timeProvider;
        _api = api;
        _dialogs = dialogs;
        _session = session;
        _stepUp = stepUp;
        _recipients = recipients;
        _recipientInitializer = recipientInitializer;
        _prefs = prefs;
        CoraLine = coraLines.GetLine("send");
    }

    public ObservableCollection<AchRecipientRow> Recipients { get; } = new();

    public ObservableCollection<string> AccountTypes { get; } = new() { "checking", "savings" };

    /// <summary>
    /// Every current ACH form validation error, in validator field order. The Send page renders
    /// the full list inline so no failure is hidden behind the first one.
    /// Use: High (every Save payee attempt). Scope: SendPage error list.
    /// </summary>
    public ObservableCollection<string> RecipientFormErrors { get; } = new();

    [ObservableProperty]
    private AchRecipientRow? selectedRecipient;

    [ObservableProperty]
    private string recipient = string.Empty;

    [ObservableProperty]
    private string amount = "25.00";

    [ObservableProperty]
    private string speed = "instant";

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private string newRecipientName = string.Empty;

    [ObservableProperty]
    private string newHolder = string.Empty;

    [ObservableProperty]
    private string newBank = string.Empty;

    [ObservableProperty]
    private string newRouting = string.Empty;

    [ObservableProperty]
    private string newAccount = string.Empty;

    [ObservableProperty]
    private string newAccountType = "checking";

    [ObservableProperty]
    private string newMemo = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasRecipientFormErrors;

    [ObservableProperty]
    private string nameError = string.Empty;

    [ObservableProperty]
    private string holderError = string.Empty;

    [ObservableProperty]
    private string bankError = string.Empty;

    [ObservableProperty]
    private string routingError = string.Empty;

    [ObservableProperty]
    private string accountError = string.Empty;

    [ObservableProperty]
    private string accountTypeError = string.Empty;

    [ObservableProperty]
    private string memoError = string.Empty;

    /// <summary>
    /// Returns the message for <paramref name="field"/>, or empty when that field is clean.
    /// Use: High (every Save payee attempt). Scope: recipient form error mapping.
    /// </summary>
    private static string MessageFor(IReadOnlyList<AchValidationError> errors, AchRecipientField field)
        => errors.FirstOrDefault(e => e.Field == field)?.Message ?? string.Empty;

    partial void OnSelectedRecipientChanged(AchRecipientRow? value)
    {
        if (value is not null)
        {
            Recipient = value.Name;
        }
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        _session.Touch();
        await _recipientInitializer.InitializeAsync();
        await RefreshRecipientsAsync();

        var prefs = await _prefs.LoadAsync();
        Speed = prefs.DefaultSendSpeed;
    }

    private async Task RefreshRecipientsAsync()
    {
        Recipients.Clear();
        foreach (var r in await _recipients.ListAsync())
        {
            Recipients.Add(r);
        }
    }

    [RelayCommand]
    private async Task AddRecipientAsync()
    {
        IReadOnlyList<AchValidationError> errors = AchRecipientValidation.ValidateDetailed(
            NewRecipientName,
            NewHolder,
            NewBank,
            NewRouting,
            NewAccount,
            NewAccountType,
            NewMemo);
        ApplyRecipientFormErrors(errors);
        if (HasRecipientFormErrors)
        {
            return;
        }

        string routing = new string(NewRouting.Where(char.IsDigit).ToArray());
        string account = NewAccount.Trim();
        AchRecipientRow row = new AchRecipientRow(
            Guid.NewGuid().ToString("N"),
            NewRecipientName.Trim(),
            NewHolder.Trim(),
            NewBank.Trim(),
            routing,
            account,
            NewAccountType.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(NewMemo) ? null : NewMemo.Trim(),
            AchRecipientValidation.MaskAccount(account),
            AchRecipientValidation.MaskRouting(routing),
            _timeProvider.GetUtcNow());
        await _recipients.UpsertAsync(row);
        Recipients.Add(row);
        SelectedRecipient = row;
        NewRecipientName = string.Empty;
        NewHolder = string.Empty;
        NewBank = string.Empty;
        NewRouting = string.Empty;
        NewAccount = string.Empty;
        NewAccountType = "checking";
        NewMemo = string.Empty;
    }

    /// <summary>
    /// Publishes validator output to the form: the summary list plus each field's inline
    /// error. Empty string means the field is clean; the set resets on every Save attempt,
    /// so a successful save clears all seven inline errors and the summary.
    /// Use: High (every Save payee attempt). Scope: SendPage recipient form.
    /// </summary>
    private void ApplyRecipientFormErrors(IReadOnlyList<AchValidationError> errors)
    {
        RecipientFormErrors.Clear();
        foreach (AchValidationError error in errors)
        {
            RecipientFormErrors.Add(error.Message);
        }

        NameError = MessageFor(errors, AchRecipientField.Name);
        HolderError = MessageFor(errors, AchRecipientField.Holder);
        BankError = MessageFor(errors, AchRecipientField.Bank);
        RoutingError = MessageFor(errors, AchRecipientField.Routing);
        AccountError = MessageFor(errors, AchRecipientField.Account);
        AccountTypeError = MessageFor(errors, AchRecipientField.AccountType);
        MemoError = MessageFor(errors, AchRecipientField.Memo);
        HasRecipientFormErrors = errors.Count > 0;
    }

    [RelayCommand]
    private async Task RemoveRecipientAsync(AchRecipientRow? recipient)
    {
        AchRecipientRow? recipientToRemove = recipient ?? SelectedRecipient;
        if (recipientToRemove is null)
        {
            return;
        }

        bool confirmed = await _dialogs.ShowConfirmAsync(
            "Remove payee",
            $"Remove {recipientToRemove.Name} from saved payees?",
            "Remove",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        await _recipients.DeleteAsync(recipientToRemove.Id);
        if (SelectedRecipient?.Id == recipientToRemove.Id)
        {
            SelectedRecipient = null;
            Recipient = string.Empty;
        }

        await RefreshRecipientsAsync();
    }

    /// <summary>
    /// Step-up authenticates then posts an ACH transfer for the selected recipient/amount.
    /// Use: High (Send confirm). Scope: SendViewModel / unlocked session.
    /// </summary>
    [RelayCommand]
    private async Task SendAsync()
    {
        _session.Touch();
        if (!_session.IsUnlocked)
        {
            await _dialogs.ShowAlertAsync("Locked", "Unlock custody before sending.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Recipient))
        {
            await _dialogs.ShowAlertAsync("Recipient", "Choose or enter a recipient.");
            return;
        }

        if (!decimal.TryParse(Amount, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal amt) || amt <= 0)
        {
            await _dialogs.ShowAlertAsync("Amount", "Enter a positive amount.");
            return;
        }

        if (!await _stepUp.RequireAsync(AuthReason.Payment))
        {
            return;
        }

        // Saved payees transfer by stable Id; free-text path keeps the typed Recipient string.
        string destination = SelectedRecipient is not null ? SelectedRecipient.Id : Recipient;
        IsBusy = true;
        try
        {
            var result = await _api.TransferAsync(destination, Amount, Speed, Guid.NewGuid().ToString("N"));
            await _dialogs.ShowAlertAsync("Send", $"{result.Id}: {result.Status}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
