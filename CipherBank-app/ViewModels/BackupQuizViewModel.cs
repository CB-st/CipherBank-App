// <copyright file="BackupQuizViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Constants;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Confirm mnemonic word recall (3 random words — Cora parity).</summary>
public partial class BackupQuizViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly OnboardingMnemonicHold _mnemonicHold;

    /// <summary>
    /// Loads the onboarding mnemonic from the hold and builds Word #N rows.
    /// Use: High (every create path). Scope: BackupQuiz page / account stories.
    /// </summary>
    public BackupQuizViewModel(INavigationService nav, OnboardingMnemonicHold mnemonicHold)
    {
        _nav = nav;
        _mnemonicHold = mnemonicHold;
        LoadFromHold();
    }

    public ObservableCollection<BackupQuizRow> Rows { get; } = new();

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string mnemonic = string.Empty;

    /// <summary>
    /// Fills Rows from the held mnemonic.
    /// Use: High (ctor / refresh). Scope: BackupQuizViewModel.
    /// </summary>
    private void LoadFromHold()
    {
        Mnemonic = _mnemonicHold.Peek() ?? string.Empty;
        Rows.Clear();
        if (string.IsNullOrWhiteSpace(Mnemonic))
        {
            return;
        }

        string[] words = MnemonicHelper.Words(Mnemonic);
        foreach (var (index, word) in BackupQuiz.PickRandom(words, 3, Random.Shared))
        {
            Rows.Add(new BackupQuizRow(index, word));
        }
    }

    /// <summary>
    /// Advances to SetPin after Word #N prompts match, keeping mnemonic in the hold.
    /// Use: High (every create path). Scope: BackupQuizPage / account stories.
    /// </summary>
    [RelayCommand]
    private async Task VerifyAsync()
    {
        Error = null;
        if (Rows.Count == 0)
        {
            Error = "Missing mnemonic.";
            return;
        }

        foreach (var row in Rows)
        {
            if (!string.Equals(row.Answer.Trim(), row.ExpectedWord, StringComparison.OrdinalIgnoreCase))
            {
                Error = "One or more words don't match. Try again.";
                return;
            }
        }

        _mnemonicHold.Set(Mnemonic);
        await _nav.GoToAsync(Routes.SetPin);
    }
}
