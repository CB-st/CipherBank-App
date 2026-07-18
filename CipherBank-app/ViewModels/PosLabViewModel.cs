// <copyright file="PosLabViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Pos;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>EMV stage chip for PosLab UI.</summary>
public partial class EmvStageItem : ObservableObject
{
    public EmvStageItem(string text, bool done)
    {
        Text = text;
        Done = done;
    }

    public string Text { get; }

    [ObservableProperty]
    private bool done;

    public string StatusLabel => Done ? "done" : "…";

    partial void OnDoneChanged(bool value) => OnPropertyChanged(nameof(StatusLabel));
}

/// <summary>POS / NFC lab — Phase D polished.</summary>
public partial class PosLabViewModel : ObservableObject
{
    private readonly IProductApi _api;
    private readonly IAppSession _session;
    private readonly INfcPresentmentService _nfc;
    private readonly IDialogService _dialogs;
    private readonly IStepUpAuth _stepUp;

    public PosLabViewModel(
        IProductApi api,
        IAppSession session,
        INfcPresentmentService nfc,
        IDialogService dialogs,
        IStepUpAuth stepUp)
    {
        _api = api;
        _session = session;
        _nfc = nfc;
        _dialogs = dialogs;
        _stepUp = stepUp;
        CoraLine = CoraLines.For("pos");
        NfcSupported = _nfc.IsSupported;
        PlatformHint = _nfc.IsSupported
            ? "Hold a writable NDEF tag to present tokenRef."
            : "NFC unavailable — use Simulate exchange (lab).";
    }

    public ObservableCollection<EmvStageItem> Stages { get; } = new();

    [ObservableProperty]
    private string? sessionId;

    [ObservableProperty]
    private string? tokenRef;

    [ObservableProperty]
    private string? last4;

    [ObservableProperty]
    private string? brand;

    [ObservableProperty]
    private string? ttlText;

    [ObservableProperty]
    private string status = "idle";

    [ObservableProperty]
    private bool nfcSupported;

    [ObservableProperty]
    private string platformHint = string.Empty;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private string? activeCardLabel;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? presentError;

    [RelayCommand]
    private void Appearing()
    {
        _session.Touch();
        NfcSupported = _nfc.IsSupported;
        string cardId = Preferences.Default.Get("pos_active_card", string.Empty);
        ActiveCardLabel = string.IsNullOrEmpty(cardId) ? "Default hardware test card" : $"Card {cardId[..Math.Min(8, cardId.Length)]}…";
    }

    [RelayCommand]
    private async Task StartSessionAsync()
    {
        _session.Touch();
        PresentError = null;
        if (!_session.IsUnlocked)
        {
            await _dialogs.ShowAlertAsync("Locked", "Unlock custody first.");
            return;
        }

        if (!await _stepUp.RequireAsync(AuthReason.PosAuthorize))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var session = await _api.CreatePosSessionAsync();
            SessionId = session.SessionId;
            Status = session.Status;

            var auth = await _api.AuthorizePosAsync(SessionId);
            TokenRef = auth.TokenRef;
            Last4 = auth.Last4;
            Brand = auth.Brand;
            TtlText = auth.TtlMs is long ttl ? $"{ttl / 1000}s TTL" : null;
            Status = auth.Status;

            var confirm = await _api.ConfirmPosAsync(SessionId);
            TokenRef = confirm.TokenRef ?? TokenRef;
            Last4 = confirm.Last4 ?? Last4;
            Brand = confirm.Brand ?? Brand;
            TtlText = confirm.TtlMs is long ttl2 ? $"{ttl2 / 1000}s TTL" : TtlText;
            Status = confirm.Status;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SimulateAsync()
    {
        _session.Touch();
        Stages.Clear();
        IsBusy = true;
        try
        {
            await foreach (string stage in EmvExchangeSimulator.RunAsync())
            {
                foreach (var existing in Stages)
                {
                    existing.Done = true;
                }

                Stages.Add(new EmvStageItem(stage, false));
            }

            foreach (var existing in Stages)
            {
                existing.Done = true;
            }

            Status = "simulated_settled";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PresentNfcAsync()
    {
        _session.Touch();
        PresentError = null;
        if (string.IsNullOrEmpty(SessionId) || string.IsNullOrEmpty(TokenRef))
        {
            await _dialogs.ShowAlertAsync("POS", "Start a session first.");
            return;
        }

        if (!_nfc.IsSupported)
        {
            PresentError = _nfc.LastError ?? "NFC unavailable — use Simulate exchange.";
            await _dialogs.ShowAlertAsync("NFC", PresentError);
            return;
        }

        if (!await _stepUp.RequireAsync(AuthReason.PosPresent))
        {
            return;
        }

        IsBusy = true;
        Status = "waiting_for_tag";
        try
        {
            bool ok = await _nfc.PresentAsync(
                new NfcPresentmentPayload
                {
                    SessionId = SessionId,
                    TokenRef = TokenRef,
                },
                TimeSpan.FromSeconds(30));
            if (ok)
            {
                Status = "presented";
                PresentError = null;
            }
            else
            {
                Status = "present_failed";
                PresentError = _nfc.LastError ?? "Presentment failed or timed out.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
