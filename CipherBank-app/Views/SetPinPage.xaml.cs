// <copyright file="SetPinPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
