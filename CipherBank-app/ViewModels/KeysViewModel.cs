// <copyright file="KeysViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Show / generate BIP39 phrase.</summary>
public partial class KeysViewModel : ObservableObject
{
    private readonly INavigationService _nav;

    public KeysViewModel(INavigationService nav)
    {
        _nav = nav;
        Mnemonic = MnemonicHelper.Generate();
    }

    [ObservableProperty]
    private string mnemonic = string.Empty;

    [RelayCommand]
    private async Task CopyAsync()
    {
        await Clipboard.Default.SetTextAsync(Mnemonic);
    }

    [RelayCommand]
    private async Task ContinueAsync()
        => await _nav.GoToAsync($"{Routes.BackupQuiz}?mnemonic={Uri.EscapeDataString(Mnemonic)}");
}
