// <copyright file="MotionSettings.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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
