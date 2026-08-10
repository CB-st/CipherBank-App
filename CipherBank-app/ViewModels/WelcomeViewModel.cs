// <copyright file="WelcomeViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Onboarding welcome.</summary>
public partial class WelcomeViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly ICustodyService _custody;

    public WelcomeViewModel(
        INavigationService nav,
        ICustodyService custody,
        ICoraLineProvider coraLines)
    {
        _nav = nav;
        _custody = custody;
        CoraLine = coraLines.GetLine("keys");
    }

    [ObservableProperty]
    private string coraLine = string.Empty;

    [RelayCommand]
    private async Task CreateWalletAsync()
        => await _nav.GoToAsync(Routes.Keys);

    [RelayCommand]
    private async Task ReturningAsync()
    {
        if (await _custody.HasSealedWalletAsync())
        {
            await _nav.GoToAsync(Routes.Unlock);
        }
        else
        {
            await _nav.GoToAsync(Routes.Keys);
        }
    }

    [RelayCommand]
    private async Task RestoreFromBackupAsync()
        => await _nav.GoToAsync(Routes.RestoreBackup);
}
