// <copyright file="ChangePinPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Change PIN page (reached from Profile → Security).</summary>
public partial class ChangePinPage : ContentPage
{
    private readonly ChangePinViewModel _vm;

    /// <summary>
    /// Builds the page and binds it to its DI-resolved ViewModel, keeping a typed reference so the
    /// disappearing hook can wipe entered PINs.
    /// Use: Low (once per navigation to Change PIN). Scope: this page instance.
    /// </summary>
    public ChangePinPage(ChangePinViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    /// <summary>
    /// Wipes entered PINs when the page leaves the screen so they never survive navigation.
    /// Use: Low (once per visit). Scope: this page instance.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.ClearSensitiveFields();
    }
}
