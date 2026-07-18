// <copyright file="AppIdleLockService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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
    private IDispatcherTimer? _timer;

    public AppIdleLockService(IAppSession session, INavigationService nav)
    {
        _session = session;
        _nav = nav;
        _session.Locked += OnLocked;
    }

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

    public void Touch() => _session.Touch();

    private void OnLocked(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _nav.GoToAsync(Routes.Unlock);
        });
    }
}
