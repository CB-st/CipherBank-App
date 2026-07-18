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
public partial class SetPinViewModel : ObservableObject, IQueryAttributable
{
    private readonly INavigationService _nav;
    private readonly IAppSession _session;

    public SetPinViewModel(INavigationService nav, IAppSession session)
    {
        _nav = nav;
        _session = session;
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("mnemonic", out object? m) && m is string s)
        {
            Mnemonic = Uri.UnescapeDataString(s);
        }
    }

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
