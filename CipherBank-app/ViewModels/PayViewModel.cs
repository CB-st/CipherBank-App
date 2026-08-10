// <copyright file="PayViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Controls;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Multi-asset pay with funding mix.</summary>
public partial class PayViewModel : ObservableObject
{
    private readonly IProductClient _api;
    private readonly IDialogService _dialogs;
    private readonly IAppSession _session;
    private readonly IStepUpAuth _stepUp;
    private readonly IThemeColorProvider _themeColors;

    public PayViewModel(
        IProductClient api,
        IDialogService dialogs,
        IAppSession session,
        IStepUpAuth stepUp,
        IThemeColorProvider themeColors,
        ICoraLineProvider coraLines)
    {
        _api = api;
        _dialogs = dialogs;
        _session = session;
        _stepUp = stepUp;
        _themeColors = themeColors;
        CoraLine = coraLines.GetLine("pay");
        RebuildMix();
    }

    public ObservableCollection<MixSource> MixSources { get; } = new();

    [ObservableProperty]
    private string amount = "2400";

    [ObservableProperty]
    private string usdShare = "60";

    [ObservableProperty]
    private string cryptoShare = "40";

    [ObservableProperty]
    private string cryptoSymbol = "DOGE";

    [ObservableProperty]
    private double mixTotal;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private string? mixError;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string recipientLabel = "Recipient · Corner Cafe";

    partial void OnAmountChanged(string value) => RebuildMix();

    partial void OnUsdShareChanged(string value) => RebuildMix();

    partial void OnCryptoShareChanged(string value) => RebuildMix();

    partial void OnCryptoSymbolChanged(string value) => RebuildMix();

    private void RebuildMix()
    {
        MixError = null;
        MixSources.Clear();
        if (!double.TryParse(Amount, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out double total) || total <= 0)
        {
            MixTotal = 0;
            MixError = "Enter a bill amount.";
            return;
        }

        if (!double.TryParse(UsdShare, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out double usdPct)
            || !double.TryParse(CryptoShare, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out double cryptoPct))
        {
            MixTotal = 0;
            MixError = "Shares must be numbers.";
            return;
        }

        if (Math.Abs((usdPct + cryptoPct) - 100) > 0.01)
        {
            MixError = "Shares must add to 100%.";
        }

        double usdVal = total * (usdPct / 100.0);
        double cryptoVal = total * (cryptoPct / 100.0);
        MixTotal = total;
        MixSources.Add(new MixSource { Asset = "USD", Value = usdVal, Color = _themeColors.Get("Success") });
        MixSources.Add(new MixSource { Asset = CryptoSymbol.ToUpperInvariant(), Value = cryptoVal, Color = _themeColors.Get("Gold") });
    }

    /// <summary>
    /// Validates the funding mix, step-up authenticates, then posts the payment.
    /// Use: High (Pay confirm). Scope: PayViewModel / unlocked session.
    /// </summary>
    [RelayCommand]
    private async Task PayAsync()
    {
        _session.Touch();
        if (!_session.IsUnlocked)
        {
            await _dialogs.ShowAlertAsync("Locked", "Unlock custody before paying.");
            return;
        }

        RebuildMix();
        if (!string.IsNullOrEmpty(MixError))
        {
            await _dialogs.ShowAlertAsync("Mix", MixError);
            return;
        }

        if (!await _stepUp.RequireAsync(AuthReason.Payment))
        {
            return;
        }

        IsBusy = true;
        try
        {
            Dictionary mix = new Dictionary<string, string>
            {
                ["USD"] = UsdShare,
                [CryptoSymbol.ToUpperInvariant()] = CryptoShare,
            };
            var result = await _api.PayAsync(Amount, mix, Guid.NewGuid().ToString("N"));
            await _dialogs.ShowAlertAsync("Pay", $"{result.Id}: {result.Status}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
