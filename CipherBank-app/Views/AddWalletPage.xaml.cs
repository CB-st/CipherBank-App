// <copyright file="AddWalletPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Add wallet page.</summary>
public partial class AddWalletPage : ContentPage
{
    public AddWalletPage(AddWalletViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
