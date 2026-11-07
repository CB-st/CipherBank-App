// <copyright file="AppIdleLockService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.Constants;
using CipherBank_app.Session;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>Watches app lifecycle and idle timeout; routes to Unlock when locked.</summary>
public sealed class AppIdleLockService
{
    // --- Idle polling ---
    private static readonly TimeSpan IdleCheckInterval = TimeSpan.FromSeconds(5);
    private static readonly Action<ILogger, Exception?> LogCleanupFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1, "CustodyLockCleanupFailure"),
        "Custody lock cleanup failed.");

    private readonly IAppSession _session;
    private readonly INavigationService _nav;
    private readonly PqChannelChallengePassStructure _pqStructure;
    private readonly TaskScheduler _taskScheduler;
    private readonly ILogger<AppIdleLockService> _logger;
    private IDispatcherTimer? _timer;

    public AppIdleLockService(
        IAppSession session,
        INavigationService nav,
        PqChannelChallengePassStructure pqStructure,
        TaskScheduler taskScheduler,
        ILogger<AppIdleLockService> logger)
    {
        _session = session;
        _nav = nav;
        _pqStructure = pqStructure;
        _taskScheduler = taskScheduler;
        _logger = logger;
        _session.Locked += OnLocked;
    }

    /// <summary>
    /// Starts the idle poll timer that calls <see cref="IAppSession.CheckIdleAndMaybeLock"/>.
    /// Use: High (once after Shell bootstrap, before Welcome/Unlock). Scope: app process lifetime.
    /// </summary>
    public void Start()
    {
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer is null)
        {
            return;
        }

        _timer.Interval = IdleCheckInterval;
        _timer.Tick += (_, _) =>
        {
            _session.CheckIdleAndMaybeLock();
        };
        _timer.Start();
    }

    /// <summary>
    /// Records user activity so the idle deadline resets (platform input + Shell navigation).
    /// Use: High (Android OnUserInteraction, Shell.Navigating, ViewModel commands). Scope: unlocked session.
    /// </summary>
    public void Touch() => _session.Touch();

    private async void OnLocked(object? sender, EventArgs e)
    {
        try
        {
            await _nav.GoToAsync(Routes.Unlock);
            await Task.Factory.StartNew(
                _pqStructure.ClearDeviceIdentity,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                _taskScheduler).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogCleanupFailure(_logger, exception);
        }
    }
}
