// <copyright file="PrefsMerge.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <summary>Merge remote prefs into local. Local keeps AssetsLayout when remote omits it.</summary>
public static class PrefsMerge
{
    public static UserPrefs Merge(UserPrefs local, PrefsWireDto? remote)
    {
        ArgumentNullException.ThrowIfNull(local);
        if (remote is null)
        {
            local.NormalizeHomeSections();
            return local;
        }

        string priorLayout = local.AssetsLayout;
        remote.FoldAlternateNames();
        bool remoteHadLayout = !string.IsNullOrWhiteSpace(remote.AssetsLayout);
        remote.ApplyOnto(local);
        if (!remoteHadLayout)
        {
            local.AssetsLayout = priorLayout;
        }

        local.NormalizeHomeSections();
        return local;
    }
}
