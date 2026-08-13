// <copyright file="CurrencyToggle.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherBank_app.ViewModels;

/// <summary>Currency visibility toggle row.</summary>
public partial class CurrencyToggle : ObservableObject
{
    public CurrencyToggle(string symbol, bool enabled)
    {
        Symbol = symbol;
        Enabled = enabled;
    }

    public string Symbol { get; }

    [ObservableProperty]
    private bool enabled;
}
