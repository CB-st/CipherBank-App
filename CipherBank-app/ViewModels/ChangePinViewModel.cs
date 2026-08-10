// <copyright file="ChangePinViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// Change the unlock PIN from Profile. Thin binder over <see cref="PinChangeCoordinator"/>: this class owns
/// only the entry fields and the surfaced error/status text, while all validation and the verify-then-replace
/// path live in Core (and are unit-tested there).
/// </summary>
public partial class ChangePinViewModel : ObservableObject
{
    /// <summary>Shown instead of an exception message so no internal detail reaches the PIN screen.</summary>
    private const string UnexpectedErrorMessage = "Could not change your PIN. Please try again.";

    private readonly PinChangeCoordinator _pinChange;
    private readonly INavigationService _nav;
    private readonly IAppSession _session;
    private readonly ILogger<ChangePinViewModel> _logger;

    public ChangePinViewModel(
        PinChangeCoordinator pinChange,
        INavigationService nav,
        IAppSession session,
        ILogger<ChangePinViewModel> logger)
    {
        _pinChange = pinChange;
        _nav = nav;
        _session = session;
        _logger = logger;
    }

    [ObservableProperty]
    private string currentPin = string.Empty;

    [ObservableProperty]
    private string newPin = string.Empty;

    [ObservableProperty]
    private string confirmPin = string.Empty;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string? status;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// Submits the change: clears prior feedback, delegates to the coordinator, and surfaces either the
    /// failure message (Error) or the success message (Status) while wiping the entry fields on success so
    /// the PINs do not linger on screen. Use: Low (one tap per change). Scope: this page instance.
    /// </summary>
    [RelayCommand]
    private async Task SubmitAsync()
    {
        _session.Touch();
        Error = null;
        Status = null;
        IsBusy = true;
        try
        {
            PinChangeOutcome outcome = await _pinChange.ChangeAsync(CurrentPin, NewPin, ConfirmPin);
            if (!outcome.Succeeded)
            {
                Error = outcome.Message;
                return;
            }

            ClearSensitiveFields();
            Status = outcome.Message;
        }
        catch (Exception ex)
        {
            LogChangePinFailed(_logger, ex);
            Error = UnexpectedErrorMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Leaves Change PIN without applying anything, wiping entered PINs first.
    /// Use: Low (cancel / done tap). Scope: this page instance.
    /// </summary>
    [RelayCommand]
    private Task CancelAsync()
    {
        ClearSensitiveFields();
        return _nav.GoToAsync(Routes.Profile);
    }

    /// <summary>
    /// Wipes the three PIN entry fields (also called when the page disappears).
    /// Use: Medium (every submit/cancel/navigate-away). Scope: this page instance.
    /// </summary>
    public void ClearSensitiveFields()
    {
        CurrentPin = string.Empty;
        NewPin = string.Empty;
        ConfirmPin = string.Empty;
    }

#pragma warning disable SA1204 // Static members should appear before non-static members - LoggerMessage source generators
    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error while changing the PIN")]
    private static partial void LogChangePinFailed(ILogger logger, Exception ex);
#pragma warning restore SA1204 // Static members should appear before non-static members
}
