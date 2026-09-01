// <copyright file="RestoreBackupPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Restore-from-backup-file page (Welcome onboarding / Unlock forgotten-PIN path).</summary>
public partial class RestoreBackupPage : ContentPage
{
    private readonly RestoreBackupViewModel _vm;

    public RestoreBackupPage(RestoreBackupViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Do not leave the recovery password in the VM after leaving this page.
        _vm.ClearSensitiveFields();
    }
}
