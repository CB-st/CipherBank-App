// <copyright file="DefaultMotionPreference.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <inheritdoc cref="IMotionPreference" />
public sealed class DefaultMotionPreference : IMotionPreference
{
    public bool IsReduceMotionEnabled => false;
}
