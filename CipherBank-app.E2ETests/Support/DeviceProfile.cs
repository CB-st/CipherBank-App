// <copyright file="DeviceProfile.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>Names the device custody state a story must establish before execution.</summary>
public enum DeviceProfile
{
    /// <summary>Represents the fresh device or story state.</summary>
    Fresh,

    /// <summary>Represents the sealed device or story state.</summary>
    Sealed,
}
