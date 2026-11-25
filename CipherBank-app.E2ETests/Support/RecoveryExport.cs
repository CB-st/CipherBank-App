// <copyright file="RecoveryExport.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// One recovery-file artifact produced by the app's own export UI: where it lives on the device, the host
/// copy kept for diagnosis, and its content digest.
/// Use: Medium (CB-ACCOUNT-002). Scope: RecoveryFileVault capture/restore.
/// </summary>
/// <param name="FileName">Display name the system document picker shows for the file.</param>
/// <param name="DevicePath">Absolute path inside the device's shared Downloads collection.</param>
/// <param name="HostPath">Host-side copy pulled for diagnosis and artifact-equivalence proof.</param>
/// <param name="Sha256">Hex SHA-256 of the exact bytes the app wrote.</param>
/// <param name="Length">Size in bytes of the exported file.</param>
public sealed record RecoveryExport(string FileName, string DevicePath, string HostPath, string Sha256, long Length);
