// <copyright file="IUserPrefs.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.ObjectModel;

namespace CipherBank_app.Persist;

/// <summary>
/// Read shape for on-device UI prefs. <see cref="IPrefsStore"/> still round-trips the concrete
/// <see cref="UserPrefs"/> so System.Text.Json can materialize the bag.
/// </summary>
public interface IUserPrefs
{
    /// <summary>Gets the ordered Home section keys (e.g. <c>holdings</c>, <c>rates</c>).</summary>
    Collection<string> HomeOrder { get; }

    /// <summary>Gets per-section Home visibility keyed by the same section keys as <see cref="HomeOrder"/>.</summary>
    Dictionary<string, bool> HomeVisible { get; }

    /// <summary>Gets the asset symbols the user enabled for display (uppercase, e.g. <c>BTC</c>).</summary>
    Collection<string> EnabledCurrencies { get; }

    /// <summary>Gets or sets the idle seconds before the session locks. Zero or negative means the host default.</summary>
    int LockIdleSeconds { get; set; }

    /// <summary>Gets or sets the appearance mode: <c>system</c>, <c>light</c>, or <c>dark</c>.</summary>
    string Appearance { get; set; }

    /// <summary>
    /// Migrates legacy section keys and fills missing home/currency defaults.
    /// Use: High (load / save). Scope: IUserPrefs implementers.
    /// </summary>
    void NormalizeHomeSections();
}
