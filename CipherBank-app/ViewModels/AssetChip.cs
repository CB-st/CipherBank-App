// <copyright file="AssetChip.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherBank_app.ViewModels;

/// <summary>Chip for receive asset selection.</summary>
public partial class AssetChip : ObservableObject
{
    public AssetChip(string symbol, bool selected)
    {
        Symbol = symbol;
        Selected = selected;
    }

    public string Symbol { get; }

    [ObservableProperty]
    private bool selected;
}
