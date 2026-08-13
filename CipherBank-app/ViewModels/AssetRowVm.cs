// <copyright file="AssetRowVm.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.ViewModels;

/// <summary>Unified asset row for combined holdings+local layout.</summary>
public sealed class AssetRowVm
{
    public string Symbol { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string Trailing { get; set; } = string.Empty;

    public Color Accent { get; set; } = Colors.Gray;

    public string KindLabel { get; set; } = string.Empty;

    public LocalWalletRow? LocalWallet { get; set; }

    public bool IsLocalWallet { get; set; }
}
