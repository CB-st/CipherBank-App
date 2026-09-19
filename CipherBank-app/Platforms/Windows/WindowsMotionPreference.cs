// <copyright file="WindowsMotionPreference.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;
using Windows.UI.ViewManagement;

namespace CipherBank_app.Platforms.Windows;

/// <inheritdoc cref="IMotionPreference" />
public sealed class WindowsMotionPreference : IMotionPreference
{
    public bool IsReduceMotionEnabled => !new UISettings().AnimationsEnabled;
}
