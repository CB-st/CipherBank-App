// <copyright file="SendPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Send page.</summary>
public partial class SendPage : ContentPage
{
    private readonly SendViewModel _vm;

    public SendPage(SendViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.AppearingCommand.ExecuteAsync(null);
    }
}
