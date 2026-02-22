// <copyright file="AppDelegate.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Foundation;

namespace CipherBank_app;

/// <summary>
/// MacCatalyst application delegate.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
