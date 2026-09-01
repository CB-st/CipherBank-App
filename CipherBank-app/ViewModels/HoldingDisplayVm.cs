// <copyright file="HoldingDisplayVm.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>


namespace CipherBank_app.ViewModels;

/// <summary>Holdings row with optional masked balances.</summary>
public sealed class HoldingDisplayVm
{
    public string Symbol { get; set; } = string.Empty;

    public string Balance { get; set; } = string.Empty;

    public string UsdValue { get; set; } = string.Empty;
}
