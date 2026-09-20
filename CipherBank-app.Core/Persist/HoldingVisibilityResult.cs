// <copyright file="HoldingVisibilityResult.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;

namespace CipherBank_app.Persist;

/// <summary>Visible and hidden Home holding collections.</summary>
public sealed record HoldingVisibilityResult(
    IReadOnlyList<HoldingDto> Visible,
    IReadOnlyList<HoldingDto> Other);
