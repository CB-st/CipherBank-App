// <copyright file="AppleMotionPreference.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;

namespace CipherBank_app.Platforms.Ios;

/// <inheritdoc cref="IMotionPreference" />
public sealed class AppleMotionPreference : IMotionPreference
{
    public bool IsReduceMotionEnabled => UIKit.UIAccessibility.IsReduceMotionEnabled;
}
