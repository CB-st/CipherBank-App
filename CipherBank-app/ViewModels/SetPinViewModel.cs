// <copyright file="SetPinViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Set PIN and seal custody blob; seed default wallets.</summary>
public partial class SetPinViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IAppSession _session;
    private readonly OnboardingMnemonicHold _mnemonicHold;

    /// <summary>
    /// Loads the onboarding mnemonic from scoped hold (never Shell query strings).
    /// Use: High (create/restore pin step). Scope: SetPin page.
    /// </summary>
    public SetPinViewModel(INavigationService nav, IAppSession session, OnboardingMnemonicHold mnemonicHold)
    {
        _nav = nav;
        _session = session;
        _mnemonicHold = mnemonicHold;
        Mnemonic = _mnemonicHold.Peek() ?? string.Empty;
    }

    [ObservableProperty]
    private string pin = string.Empty;

    [ObservableProperty]
    private string confirmPin = string.Empty;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string mnemonic = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// Seals custody with the held mnemonic then clears the hold.
    /// Use: High (finish create/restore). Scope: SetPin page.
    /// </summary>
    [RelayCommand]
    private async Task SealAsync()
    {
        Error = null;
        if (Pin.Length < 6)
        {
            Error = "PIN must be at least 6 digits.";
            return;
        }

        if (Pin != ConfirmPin)
        {
            Error = "PINs do not match.";
            return;
        }

        if (!MnemonicHelper.Validate(Mnemonic))
        {
            Error = "Invalid recovery phrase.";
            return;
        }

        IsBusy = true;
        try
        {
            await _session.FinishCustodySetupAsync(Mnemonic, Pin);
            _mnemonicHold.Clear();
            Mnemonic = string.Empty;
            await _nav.GoToAsync(Routes.Home);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
