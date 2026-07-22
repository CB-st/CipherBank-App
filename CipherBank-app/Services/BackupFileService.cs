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
        string path = Path.Combine(FileSystem.CacheDirectory, suggestedFileName);
        await File.WriteAllBytesAsync(path, fileBytes, ct).ConfigureAwait(false);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Save CipherBank recovery file",
            File = new ShareFile(path),
        }).ConfigureAwait(false);

        return true;
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
