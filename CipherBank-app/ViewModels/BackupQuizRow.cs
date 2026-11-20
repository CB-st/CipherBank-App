// <copyright file="BackupQuizRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherBank_app.ViewModels;

/// <summary>One quiz row for the 3-word backup confirmation.</summary>
public partial class BackupQuizRow(int index, string expectedWord) : ObservableObject
{
    public int Index { get; } = index;

    public string ExpectedWord { get; } = expectedWord;

    public string Prompt { get; } = $"Word #{index + 1}";

    [ObservableProperty]
    private string answer = string.Empty;
}
