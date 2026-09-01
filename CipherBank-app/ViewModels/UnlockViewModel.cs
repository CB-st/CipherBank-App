// <copyright file="UnlockViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Net.WebSockets;
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
        ISettingsService settings,
        ICoraLineProvider coraLines)
    {
        _nav = nav;
        _session = session;
        _pin = pin;
        _biometrics = biometrics;
        _settings = settings;
        CoraLine = coraLines.GetLine("unlock");
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

    /// <summary>
    /// Unlocks custody with the entered PIN, then navigates Home on success.
    /// Use: High (PIN unlock). Scope: UnlockViewModel / AppSession.
    /// </summary>
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
        catch (Exception ex) when (IsUnlockTransportFailure(ex))
        {
            Error = "Could not complete unlock. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Unlocks via device-owner biometrics, then navigates Home on success.
    /// Use: Medium (auto-prompt / biometric button). Scope: UnlockViewModel / AppSession.
    /// </summary>
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
        catch (Exception ex) when (IsUnlockTransportFailure(ex))
        {
            Error = "Could not complete unlock. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// True when the failure is an HTTP/stream/timeout escape past AppSession rollback.
    /// Use: Low (unlock failure path). Scope: UnlockViewModel.
    /// </summary>
    private static bool IsUnlockTransportFailure(Exception ex)
        => ex is HttpRequestException
            or WebSocketException
            or TaskCanceledException
            or OperationCanceledException
            or InvalidOperationException
            or UnauthorizedAccessException;

    [RelayCommand]
    private async Task RestoreFromBackupAsync()
        => await _nav.GoToAsync(Routes.RestoreBackup);
}
