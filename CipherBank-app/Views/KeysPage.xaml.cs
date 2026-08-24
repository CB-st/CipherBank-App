// <copyright file="KeysPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Recovery phrase page.</summary>
public partial class KeysPage : ContentPage
{
    public KeysPage(KeysViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
