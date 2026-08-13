// <copyright file="AndroidNdefPresentmentService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

#if ANDROID
using Android.Nfc;
using Android.Nfc.Tech;
using CipherBank_app.Pos;

namespace CipherBank_app.Platforms.Android.Nfc;

/// <summary>Android NDEF presentment via Reader Mode (Cora nfcPresent parity).</summary>
public sealed class AndroidNdefPresentmentService : INfcPresentmentService
{
    public bool IsSupported
    {
        get
        {
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return false;
            }

            var adapter = NfcAdapter.GetDefaultAdapter(activity);
            return adapter is { IsEnabled: true };
        }
    }

    public string? LastError { get; private set; }

    public async Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan timeout, CancellationToken ct)
    {
        LastError = null;
        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            LastError = "No active Android activity.";
            return false;
        }

        var adapter = NfcAdapter.GetDefaultAdapter(activity);
        if (adapter is null || !adapter.IsEnabled)
        {
            LastError = "NFC adapter unavailable or disabled.";
            return false;
        }

        string json = payload.ToJson();
        var message = new NdefMessage(new[]
        {
            NdefRecord.CreateTextRecord("en", json)!,
        });

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new NdefWriterCallback(tag =>
        {
            try
            {
                using var ndef = Ndef.Get(tag);
                if (ndef is null)
                {
                    LastError = "Tag does not support NDEF.";
                    tcs.TrySetResult(false);
                    return;
                }

                ndef.Connect();
                if (!ndef.IsWritable)
                {
                    LastError = "Tag is not writable.";
                    ndef.Close();
                    tcs.TrySetResult(false);
                    return;
                }

                ndef.WriteNdefMessage(message);
                ndef.Close();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                tcs.TrySetResult(false);
            }
            finally
            {
                try
                {
                    adapter.DisableReaderMode(activity);
                }
                catch
                {
                    // ignored
                }
            }
        });

        var flags = NfcReaderFlags.NfcA | NfcReaderFlags.NfcB | NfcReaderFlags.NfcF | NfcReaderFlags.NfcV;
        adapter.EnableReaderMode(activity, callback, flags, null);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);
        await using var reg = linked.Token.Register(() =>
        {
            try
            {
                adapter.DisableReaderMode(activity);
            }
            catch
            {
                // ignored
            }

            if (LastError is null)
            {
                LastError = "NFC presentment timed out or was cancelled.";
            }

            tcs.TrySetResult(false);
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    private sealed class NdefWriterCallback : Java.Lang.Object, NfcAdapter.IReaderCallback
    {
        private readonly Action<Tag> _onTag;

        public NdefWriterCallback(Action<Tag> onTag) => _onTag = onTag;

        public void OnTagDiscovered(Tag? tag)
        {
            if (tag is not null)
            {
                _onTag(tag);
            }
        }
    }
}
#endif
