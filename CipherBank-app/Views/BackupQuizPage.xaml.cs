// <copyright file="BackupQuizPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Confirm backup page.</summary>
public partial class BackupQuizPage : ContentPage
{
    public BackupQuizPage(BackupQuizViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
