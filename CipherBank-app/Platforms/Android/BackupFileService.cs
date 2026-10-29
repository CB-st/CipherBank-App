// <copyright file="BackupFileService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <inheritdoc />
public sealed partial class BackupFileService
{
    private const string RecoveryMimeType = "application/json";

    /// <summary>
    /// Publishes the recovery file into the shared Downloads collection through MediaStore, so it survives
    /// app-data resets and is reachable from the system document picker on restore. Returns null on
    /// pre-scoped-storage devices (API &lt; 29), where the caller falls back to share-only.
    /// Use: Low (once per export). Scope: device-wide Downloads collection.
    /// </summary>
    private static async partial Task<string?> SaveToSharedStorageAsync(byte[] fileBytes, string fileName, CancellationToken ct)
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
}
