// <copyright file="CardTransform.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Animations;

/// <summary>
/// The visual transform for a single carousel card, as a function of its
/// signed distance from the centered position. UI-agnostic (plain numbers).
/// </summary>
public readonly record struct CardTransform(
    double TranslationX,
    double TranslationY,
    double RotationY,
    double Scale,
    double Opacity,
    int ZIndex);
