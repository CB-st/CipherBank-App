// <copyright file="ChangePinPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Change PIN page (reached from Profile → Security).</summary>
public partial class ChangePinPage : ContentPage
{
    private readonly ChangePinViewModel _vm;

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
