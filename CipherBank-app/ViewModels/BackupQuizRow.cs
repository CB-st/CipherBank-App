// <copyright file="BackupQuizRow.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;

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
