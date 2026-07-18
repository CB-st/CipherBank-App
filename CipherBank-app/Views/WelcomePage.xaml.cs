// <copyright file="WelcomePage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Welcome page.</summary>
public partial class WelcomePage : ContentPage
{
    public WelcomePage(WelcomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
