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
    Collection<string> HomeOrder { get; }

    Dictionary<string, bool> HomeVisible { get; }

    Collection<string> EnabledCurrencies { get; }

    int LockIdleSeconds { get; set; }

    string Appearance { get; set; }

    /// <summary>
    /// Migrates legacy section keys and fills missing home/currency defaults.
    /// Use: High (load / save). Scope: IUserPrefs implementers.
    /// </summary>
    void NormalizeHomeSections();
}
