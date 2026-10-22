// <copyright file="AppShell.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app;

/// <summary>
/// The application shell providing navigation structure. All pages are part of
/// the shell visual hierarchy declared in XAML, so no global route
/// registrations are needed (and duplicating hierarchy routes via
/// Routing.RegisterRoute risks "Ambiguous routes matched" failures).
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }
}
