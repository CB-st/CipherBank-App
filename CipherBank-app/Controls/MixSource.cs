// <copyright file="MixSource.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>


namespace CipherBank_app.Controls;

/// <summary>One funding source in a pay mix.</summary>
public sealed class MixSource
{
    public string Asset { get; set; } = string.Empty;

    public double Value { get; set; }

    public Color Color { get; set; } = Colors.DodgerBlue;
}
