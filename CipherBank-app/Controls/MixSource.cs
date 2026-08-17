// <copyright file="MixSource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Controls;

/// <summary>One funding source in a pay mix.</summary>
public sealed class MixSource
{
    public string Asset { get; set; } = string.Empty;

    public double Value { get; set; }

    public Color Color { get; set; } = Colors.DodgerBlue;
}
