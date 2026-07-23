// <copyright file="BackupFileService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <inheritdoc />
public sealed class BackupFileService : IBackupFileService
{
    public async Task<bool> SaveAndShareAsync(byte[] fileBytes, string suggestedFileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        string dir = Path.Combine(FileSystem.CacheDirectory, "recovery-export");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, suggestedFileName);
        await File.WriteAllBytesAsync(path, fileBytes, ct).ConfigureAwait(false);

        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Save CipherBank recovery file",
                File = new ShareFile(path),
            }).ConfigureAwait(false);

            return true;
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort: share sheet may briefly retain the file handle.
            }
        }
    }

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
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        return buffer.ToArray();
    }
}
