// <copyright file="SetPinPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Set PIN page.</summary>
public partial class SetPinPage : ContentPage
{
    public SetPinPage(SetPinViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
