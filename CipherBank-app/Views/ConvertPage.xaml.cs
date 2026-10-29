// <copyright file="ConvertPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Convert page.</summary>
public partial class ConvertPage : ContentPage
{
    public ConvertPage(ConvertViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
