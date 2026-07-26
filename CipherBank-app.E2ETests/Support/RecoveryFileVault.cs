using System.Security.Cryptography;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// One recovery-file artifact produced by the app's own export UI: where it lives on the device, the host
/// copy kept for diagnosis, and its content digest.
/// </summary>
/// <param name="FileName">Display name the system document picker shows for the file.</param>
/// <param name="DevicePath">Absolute path inside the device's shared Downloads collection.</param>
/// <param name="HostPath">Host-side copy pulled for diagnosis and artifact-equivalence proof.</param>
/// <param name="Sha256">Hex SHA-256 of the exact bytes the app wrote.</param>
/// <param name="Length">Size in bytes of the exported file.</param>
public sealed record RecoveryExport(string FileName, string DevicePath, string HostPath, string Sha256, long Length);

/// <summary>
/// Owns the exported recovery file for one story run. The bytes are always produced by the app's real
/// export path (<c>Profile → Create and save backup file</c> → Core <c>IMnemonicBackupService</c> →
/// MediaStore Downloads); this object only finds that file, keeps a host copy, and — should a device reset
/// ever remove it — puts the *same* bytes back so the app's real document picker can open them again.
/// It never creates, decrypts or rewrites recovery content.
/// Use: Medium (a few adb calls per recovery story). Scope: one story run's export artifact.
/// </summary>
public sealed class RecoveryFileVault
{
    private const string DeviceDownloadsDir = "/sdcard/Download";
    private const string ExportPrefix = "cipherbank-recovery-";
    private const string ExportSuffix = ".cbr.json";
    private const string MediaVolumeUri = "content://media/external";
    private const string PrimaryVolume = "external_primary";

    private static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private readonly string _hostDir;

    /// <summary>
    /// Resolves the host artifact directory (E2E_RECOVERY_DIR, default <c>artifacts/e2e-recovery</c>) that
    /// holds pulled copies of exported files. Use: Medium (once per story). Scope: this vault instance.
    /// </summary>
    public RecoveryFileVault(string? hostDir = null)
    {
        _hostDir = RepoPaths.ResolveFromRoot(
            hostDir ?? Environment.GetEnvironmentVariable("E2E_RECOVERY_DIR") ?? "artifacts/e2e-recovery");
    }

    /// <summary>The export captured by <see cref="CaptureExport"/>, or null before the app has exported one.</summary>
    public RecoveryExport? Captured { get; private set; }

    /// <summary>
    /// Removes recovery files left on the device by earlier runs so a capture cannot pick up a stale export
    /// and silently prove nothing. Use: Medium (once per recovery story, before the export).
    /// Scope: the device Downloads collection.
    /// </summary>
    public void ClearDeviceExports()
    {
        foreach (string name in ListDeviceExports())
        {
            Adb.Shell($"rm -f {DeviceDownloadsDir}/{name}");
        }

        RescanSharedStorage();
    }

    /// <summary>
    /// Waits for the app's export to land in shared Downloads, pulls a host copy and hashes it. Throws when
    /// no export appears inside <see cref="ExportTimeout"/> — a missing file means the export UI did not
    /// complete, which must fail the story rather than be worked around.
    /// Use: Medium (once per recovery story). Scope: this vault instance.
    /// </summary>
    public RecoveryExport CaptureExport()
    {
        string name = WaitForExportName();
        Directory.CreateDirectory(_hostDir);
        string devicePath = $"{DeviceDownloadsDir}/{name}";
        string hostPath = Path.Combine(_hostDir, name);

        string pullOutput = Adb.Run($"pull {devicePath} \"{hostPath}\"");
        if (!File.Exists(hostPath))
        {
            throw new InvalidOperationException($"adb pull of {devicePath} produced no host copy. Output: {pullOutput}");
        }

        byte[] bytes = File.ReadAllBytes(hostPath);
        Captured = new RecoveryExport(
            name,
            devicePath,
            hostPath,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            bytes.LongLength);
        return Captured;
    }

    /// <summary>
    /// Re-asserts that the captured export is present in the device's Downloads collection after a device
    /// reset, pushing the identical host copy back and re-scanning the volume only if the reset removed it.
    /// Returns true when a re-push was needed, so the story can journal what actually happened.
    /// Use: Medium (once per recovery story, right after Fresh). Scope: the device Downloads collection.
    /// </summary>
    public bool EnsureOnDevice()
    {
        RecoveryExport export = Captured
            ?? throw new InvalidOperationException("EnsureOnDevice called before the app exported a recovery file.");

        if (ListDeviceExports().Contains(export.FileName))
        {
            return false;
        }

        Adb.Run($"push \"{export.HostPath}\" {export.DevicePath}");
        RescanSharedStorage();
        if (!ListDeviceExports().Contains(export.FileName))
        {
            throw new InvalidOperationException(
                $"Could not restore {export.FileName} to {DeviceDownloadsDir} after the device reset.");
        }

        return true;
    }

    /// <summary>
    /// Lists recovery-file names currently in the device Downloads directory, newest last-written first.
    /// Use: Medium (capture + restore checks). Scope: the device Downloads collection.
    /// </summary>
    public IReadOnlyList<string> ListDeviceExports() =>
        Adb.ShellLines($"ls -1t {DeviceDownloadsDir} 2>/dev/null")
            .Where(line => line.StartsWith(ExportPrefix, StringComparison.Ordinal)
                           && line.EndsWith(ExportSuffix, StringComparison.Ordinal))
            .ToArray();

    /// <summary>
    /// Polls the Downloads directory until the app's export appears (the export runs on the device's own
    /// schedule: step-up, 600k-iteration KDF, then the MediaStore write).
    /// Use: Medium (once per capture). Scope: this vault instance.
    /// </summary>
    private string WaitForExportName()
    {
        DateTime deadline = DateTime.UtcNow + ExportTimeout;
        while (DateTime.UtcNow < deadline)
        {
            string? found = ListDeviceExports().FirstOrDefault();
            if (found is not null)
            {
                return found;
            }

            Thread.Sleep(PollInterval);
        }

        throw new TimeoutException(
            $"No {ExportPrefix}*{ExportSuffix} appeared in {DeviceDownloadsDir} within "
            + $"{ExportTimeout.TotalSeconds:0}s — the in-app export did not complete.");
    }

    /// <summary>
    /// Asks MediaProvider to re-index the primary shared volume so a file placed by adb becomes visible to
    /// the system document picker (which lists MediaStore rows, not raw directory contents).
    /// Use: Low (only around clear/restore). Scope: the device's primary shared volume.
    /// </summary>
    private static void RescanSharedStorage() =>
        Adb.Shell($"content call --uri {MediaVolumeUri} --method scan_volume --arg {PrimaryVolume}");
}
