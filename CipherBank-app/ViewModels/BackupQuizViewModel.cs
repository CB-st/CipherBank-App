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

/// <summary>One quiz row for the 3-word backup confirmation.</summary>
public partial class BackupQuizRow : ObservableObject
{
    public BackupQuizRow(int index, string expectedWord)
    {
        Index = index;
        ExpectedWord = expectedWord;
        Prompt = $"Word #{index + 1}";
    }

    public int Index { get; }

    public string ExpectedWord { get; }

    public string Prompt { get; }

    [ObservableProperty]
    private string answer = string.Empty;
}

/// <summary>Confirm mnemonic word recall (3 random words — Cora parity).</summary>
public partial class BackupQuizViewModel : ObservableObject, IQueryAttributable
{
    private readonly INavigationService _nav;

    public BackupQuizViewModel(INavigationService nav) => _nav = nav;

    public ObservableCollection<BackupQuizRow> Rows { get; } = new();

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string mnemonic = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("mnemonic", out object? m) && m is string s)
        {
            Mnemonic = Uri.UnescapeDataString(s);
            string[] words = MnemonicHelper.Words(Mnemonic);
            Rows.Clear();
            foreach (var (index, word) in BackupQuiz.PickRandom(words, 3, Random.Shared))
            {
                Rows.Add(new BackupQuizRow(index, word));
            }
        }
    }

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

        await _nav.GoToAsync($"{Routes.SetPin}?mnemonic={Uri.EscapeDataString(Mnemonic)}");
    }
}
