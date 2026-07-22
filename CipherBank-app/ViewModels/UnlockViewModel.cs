// <copyright file="UnlockViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Unlock sealed wallet with PIN or OS biometrics (device-secret path).</summary>
public partial class UnlockViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IAppSession _session;
    private readonly IPinService _pin;
    private readonly IBiometricService _biometrics;
    private readonly ISettingsService _settings;
    private bool _autoPrompted;

    public UnlockViewModel(
        INavigationService nav,
        IAppSession session,
        IPinService pin,
        IBiometricService biometrics,
        ISettingsService settings)
    {
        _nav = nav;
        _session = session;
        _pin = pin;
        _biometrics = biometrics;
        _settings = settings;
        CoraLine = CoraLines.For("unlock");
    }

    [ObservableProperty]
    private string pin = string.Empty;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool biometricsAvailable;

    [RelayCommand]
    private async Task AppearingAsync()
    {
        await _pin.RefreshAsync();
        if (_pin.IsLockedOut)
        {
            Error = $"Locked out. Try again in {_pin.LockoutRemaining?.TotalSeconds:0}s.";
        }

        BiometricsAvailable = _settings.BiometricAuthEnabled
            && await _session.CanUnlockWithDeviceOwnerAsync()
            && await _biometrics.IsAvailableAsync();

        if (!_pin.IsLockedOut && BiometricsAvailable && !_autoPrompted)
        {
            _autoPrompted = true;
            await UnlockWithBiometricsAsync();
        }
    }

    [RelayCommand]
    private async Task UnlockAsync()
    {
        Error = null;
        await _pin.RefreshAsync();
        if (_pin.IsLockedOut)
        {
            Error = $"Locked out. Try again in {_pin.LockoutRemaining?.TotalSeconds:0}s.";
            return;
        }

        IsBusy = true;
        try
        {
            if (!await _session.UnlockAsync(Pin))
            {
                Error = "Incorrect PIN.";
                return;
            }

            await _nav.GoToAsync(Routes.Home);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UnlockWithBiometricsAsync()
    {
        Error = null;
        IsBusy = true;
        try
        {
            if (!await _biometrics.AuthenticateAsync("Unlock CipherBank"))
            {
                Error = "Biometric authentication failed.";
                return;
            }

            if (!await _session.UnlockWithDeviceOwnerAsync())
            {
                Error = "Could not open the sealed wallet. Enter your PIN.";
                return;
            }

            await _nav.GoToAsync(Routes.Home);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreFromBackupAsync()
        => await _nav.GoToAsync(Routes.RestoreBackup);
}
