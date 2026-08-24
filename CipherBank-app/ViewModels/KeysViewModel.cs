// <copyright file="KeysViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Show / generate BIP39 phrase.</summary>
public partial class KeysViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly OnboardingMnemonicHold _mnemonicHold;

    /// <summary>
    /// Generates a fresh mnemonic for the create-wallet path.
    /// Use: High (every create flow). Scope: Keys page / onboarding.
    /// </summary>
    public KeysViewModel(
        INavigationService nav,
        OnboardingMnemonicHold mnemonicHold,
        ICoraLineProvider coraLines)
    {
        _nav = nav;
        _mnemonicHold = mnemonicHold;
        Mnemonic = MnemonicHelper.Generate();
        CoraLine = coraLines.GetLine("keys");
    }

    [ObservableProperty]
    private string mnemonic = string.Empty;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [RelayCommand]
    private async Task CopyAsync()
    {
        await Clipboard.Default.SetTextAsync(Mnemonic);
    }

    /// <summary>
    /// Parks the mnemonic in scoped hold, clears this page's copy, and opens BackupQuiz (no route query).
    /// Use: High (continue). Scope: Keys → BackupQuiz handoff.
    /// </summary>
    [RelayCommand]
    private async Task ContinueAsync()
    {
        _mnemonicHold.Set(Mnemonic);
        Mnemonic = string.Empty;
        await _nav.GoToAsync(Routes.BackupQuiz);
    }
}
