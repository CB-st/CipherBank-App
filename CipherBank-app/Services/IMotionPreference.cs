// <copyright file="IMotionPreference.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Exposes the active platform's reduced-motion accessibility preference.</summary>
public interface IMotionPreference
{
    bool IsReduceMotionEnabled { get; }
}
