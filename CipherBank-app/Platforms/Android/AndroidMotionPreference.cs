// <copyright file="AndroidMotionPreference.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;

namespace CipherBank_app.Platforms.Android;

/// <inheritdoc cref="IMotionPreference" />
public sealed class AndroidMotionPreference : IMotionPreference
{
    public bool IsReduceMotionEnabled => false;
}
