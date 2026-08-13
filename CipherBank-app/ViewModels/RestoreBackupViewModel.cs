// <copyright file="RestoreBackupViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>
/// Restore a wallet from a ciphered mnemonic recovery file (Welcome onboarding, or
/// Unlock "forgotten PIN" path when a sealed wallet is already present).
/// </summary>
public partial class RestoreBackupViewModel : ObservableObject
{
    private const int MinRecoveryPasswordLength = 12;

    private readonly IMnemonicBackupService _backup;
    private readonly IBackupFileService _backupFiles;
    private readonly ICustodyService _custody;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly OnboardingMnemonicHold _mnemonicHold;

    private byte[]? _fileBytes;

    public RestoreBackupViewModel(
        IMnemonicBackupService backup,
        IBackupFileService backupFiles,
        ICustodyService custody,
        INavigationService nav,
        IDialogService dialogs,
        OnboardingMnemonicHold mnemonicHold,
        ICoraLineProvider coraLines)
    {
        _backup = backup;
        _backupFiles = backupFiles;
        _custody = custody;
        _nav = nav;
        _dialogs = dialogs;
        _mnemonicHold = mnemonicHold;
        CoraLine = coraLines.GetLine("keys");
    }

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private string? fileStatus;

    [ObservableProperty]
    private string recoveryPassword = string.Empty;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private async Task PickFileAsync()
    {
        Error = null;
        byte[]? bytes = await _backupFiles.PickBackupFileAsync();
        if (bytes is null)
        {
            return;
        }

        _fileBytes = bytes;
        FileStatus = "Recovery file selected.";
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        Error = null;
        if (_fileBytes is null)
        {
            Error = "Choose a recovery file first.";
            return;
        }

        if (RecoveryPassword.Length < MinRecoveryPasswordLength)
        {
            Error = $"Recovery password must be at least {MinRecoveryPasswordLength} characters.";
            return;
        }

        IsBusy = true;
        string mnemonic;
        try
        {
            mnemonic = await _backup.OpenBackupFileAsync(_fileBytes, RecoveryPassword);
        }
        catch (Exception)
        {
            Error = "Incorrect recovery password, or the file is unreadable.";
            return;
        }
        finally
        {
            RecoveryPassword = string.Empty;
            IsBusy = false;
        }

        if (await _custody.HasSealedWalletAsync())
        {
            bool confirmed = await _dialogs.ShowConfirmAsync(
                "Replace wallet seal",
                "This replaces the on-device wallet seal and regenerates derived receive addresses from the recovered seed. Continue?",
                "Replace",
                "Cancel");
            if (!confirmed)
            {
                return;
            }
        }

        _mnemonicHold.Set(mnemonic);
        await _nav.GoToAsync(Routes.SetPin);
    }

    /// <summary>Clears the recovery password (call when leaving the page too).</summary>
    public void ClearSensitiveFields()
    {
        RecoveryPassword = string.Empty;
    }
}
