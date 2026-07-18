// <copyright file="BackupQuizViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Confirm mnemonic word recall.</summary>
public partial class BackupQuizViewModel : ObservableObject, IQueryAttributable
{
    private readonly INavigationService _nav;
    private string[] _words = Array.Empty<string>();
    private int _index;

    public BackupQuizViewModel(INavigationService nav) => _nav = nav;

    [ObservableProperty]
    private string prompt = string.Empty;

    [ObservableProperty]
    private string answer = string.Empty;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string mnemonic = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("mnemonic", out object? m) && m is string s)
        {
            Mnemonic = Uri.UnescapeDataString(s);
            _words = MnemonicHelper.Words(Mnemonic);
            _index = Math.Min(2, _words.Length - 1);
            Prompt = $"Enter word #{_index + 1}";
        }
    }

    [RelayCommand]
    private async Task VerifyAsync()
    {
        Error = null;
        if (_words.Length == 0)
        {
            Error = "Missing mnemonic.";
            return;
        }

        if (!string.Equals(Answer.Trim(), _words[_index], StringComparison.OrdinalIgnoreCase))
        {
            Error = "That word doesn't match. Try again.";
            return;
        }

        await _nav.GoToAsync($"{Routes.SetPin}?mnemonic={Uri.EscapeDataString(Mnemonic)}");
    }
}
