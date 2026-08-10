// <copyright file="AppIdleLockService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.Constants;
using CipherBank_app.Session;

namespace CipherBank_app.Services;

/// <summary>Watches app lifecycle and idle timeout; routes to Unlock when locked.</summary>
public sealed class AppIdleLockService
{
    // --- Idle polling ---
    private static readonly TimeSpan IdleCheckInterval = TimeSpan.FromSeconds(5);

    private readonly IAppSession _session;
    private readonly INavigationService _nav;
    private readonly PqChannelChallengePassStructure _pqStructure;
    private IDispatcherTimer? _timer;

    public AppIdleLockService(
        IAppSession session,
        INavigationService nav,
        PqChannelChallengePassStructure pqStructure)
    {
        _session = session;
        _nav = nav;
        _pqStructure = pqStructure;
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

    private void OnLocked(object? sender, EventArgs e)
    {
        // ClearDeviceIdentity waits on the A2 build gate (held across network).
        // Never block the session Locked callback / UI thread on that wait — park
        // the wipe on a worker, then navigate to Unlock once identity is gone.
        _ = Task.Run(() =>
        {
            _pqStructure.ClearDeviceIdentity();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _nav.GoToAsync(Routes.Unlock);
            });
        });
    }
}
