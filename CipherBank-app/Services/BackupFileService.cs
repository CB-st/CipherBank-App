// <copyright file="BackupFileService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <inheritdoc />
public sealed class BackupFileService : IBackupFileService
{
    private const string ShareStagingFolder = "recovery-export";
    private const string RecoveryMimeType = "application/json";
    private const string ShareSheetTitle = "Save CipherBank recovery file";

    /// <inheritdoc />
    public Task<string?> SaveRecoveryFileAsync(
        byte[] fileBytes,
        string suggestedFileName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return SaveToSharedStorageAsync(fileBytes, suggestedFileName, ct);
    }

    /// <inheritdoc />
    public async Task ShareRecoveryFileAsync(
        byte[] fileBytes,
        string suggestedFileName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        string path = await StageForShareAsync(fileBytes, suggestedFileName, ct).ConfigureAwait(false);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = ShareSheetTitle,
            File = new ShareFile(path),
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<byte[]?> PickBackupFileAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        FileResult? result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select CipherBank recovery file",
        }).ConfigureAwait(false);

        if (result is null)
        {
            return null;
        }

        using Stream stream = await result.OpenReadAsync().ConfigureAwait(false);
        using MemoryStream buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>
    /// Writes the copy the share sheet reads from, first clearing any earlier staged export so at most one
    /// encrypted file lingers in app cache. The staged copy deliberately outlives the share call: on Android
    /// <see cref="Share"/> returns as soon as the chooser launches, so deleting it here would pull the file
    /// out from under the app the user picks.
    /// Use: Low (only on the opt-in share path). Scope: app cache directory.
    /// </summary>
    private static async Task<string> StageForShareAsync(byte[] fileBytes, string fileName, CancellationToken ct)
    {
        string dir = Path.Combine(FileSystem.CacheDirectory, ShareStagingFolder);
        Directory.CreateDirectory(dir);
        PruneStagedExports(dir);

        string path = Path.Combine(dir, fileName);
        await File.WriteAllBytesAsync(path, fileBytes, ct).ConfigureAwait(false);
        return path;
    }

    /// <summary>
    /// Best-effort delete of previously staged exports; a file still held by a share target is left alone.
    /// Use: Low (once per share). Scope: the staging directory only.
    /// </summary>
    private static void PruneStagedExports(string dir)
    {
        foreach (string stale in Directory.EnumerateFiles(dir))
        {
            try
            {
                File.Delete(stale);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best-effort: a share target may still hold the handle.
            }
        }
    }

#if ANDROID

    /// <summary>
    /// Publishes the recovery file into the shared Downloads collection through MediaStore, so it survives
    /// app-data resets and is reachable from the system document picker on restore. Returns null on
    /// pre-scoped-storage devices (API &lt; 29), where the caller falls back to share-only.
    /// Use: Low (once per export). Scope: device-wide Downloads collection.
    /// </summary>
    private static async Task<string?> SaveToSharedStorageAsync(byte[] fileBytes, string fileName, CancellationToken ct)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            return null;
        }

        Android.Content.ContentResolver? resolver = Android.App.Application.Context.ContentResolver;
        Android.Net.Uri? collection = Android.Provider.MediaStore.Downloads.ExternalContentUri;
        if (resolver is null || collection is null)
        {
            return null;
        }

        Android.Content.ContentValues values = new Android.Content.ContentValues();
        values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, RecoveryMimeType);
        values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);

        Android.Net.Uri? target = resolver.Insert(collection, values);
        if (target is null)
        {
            return null;
        }

        await using Stream? output = resolver.OpenOutputStream(target, "w");
        if (output is null)
        {
            return null;
        }

        await output.WriteAsync(fileBytes, ct).ConfigureAwait(false);
        await output.FlushAsync(ct).ConfigureAwait(false);
        return $"{Android.OS.Environment.DirectoryDownloads}/{fileName}";
    }

#else

    /// <summary>
    /// Non-Android hosts have no shared-storage collection wired up yet, so the export finishes through the
    /// share sheet alone. Use: Low (once per export). Scope: this platform build.
    /// </summary>
    private static Task<string?> SaveToSharedStorageAsync(byte[] fileBytes, string fileName, CancellationToken ct)
        => Task.FromResult<string?>(null);

#endif
}
