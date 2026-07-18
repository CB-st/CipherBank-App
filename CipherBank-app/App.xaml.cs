// <copyright file="App.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;

namespace CipherBank_app;

/// <summary>
/// The main application class.
/// </summary>
public partial class App : Application
{
    private readonly IAppSession _session;
    private readonly ILocalDb _db;
    private readonly AppIdleLockService _idleLock;

    public App(IAppSession session, ILocalDb db, AppIdleLockService idleLock)
    {
        InitializeComponent();
        _session = session;
        _db = db;
        _idleLock = idleLock;
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_session, _db, _idleLock));
    }
}
