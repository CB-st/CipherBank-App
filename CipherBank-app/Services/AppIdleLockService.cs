// <copyright file="AppIdleLockService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
        // Navigate to Unlock first so the UI never waits on ClearDeviceIdentity
        // (A2 build gate / network). Wipe device identity on a worker afterward.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _nav.GoToAsync(Routes.Unlock);
        });
        _ = Task.Run(_pqStructure.ClearDeviceIdentity);
    }
}
