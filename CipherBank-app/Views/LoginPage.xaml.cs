// <copyright file="LoginPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>
/// Code-behind for the Login page.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnUsernameCompleted(object? sender, EventArgs e)
    {
        // Move focus to password field when Enter is pressed on username
        PasswordEntry.Focus();
    }

    private void OnPasswordCompleted(object? sender, EventArgs e)
    {
        // Submit login when Enter is pressed on password field
        if (_viewModel.SignInCommand.CanExecute(null))
        {
            _viewModel.SignInCommand.Execute(null);
        }
    }
}
