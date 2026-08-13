// <copyright file="MotionSettings.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Controls;

/// <summary>
/// Exposes the OS reduce-motion preference where the platform reports it.
/// </summary>
internal static class MotionSettings
{
    public static bool ReduceMotion =>
#if IOS || MACCATALYST
        UIKit.UIAccessibility.IsReduceMotionEnabled;
#else
        false;
#endif
}
