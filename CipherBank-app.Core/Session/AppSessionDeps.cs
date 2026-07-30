// <copyright file="AppSessionDeps.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using CipherBank_app.Wallets;

namespace CipherBank_app.Session;

/// <summary>Constructor dependencies for <see cref="AppSession"/>.</summary>
public readonly record struct AppSessionDeps(
    ICustodyService Custody,
    IProductApi Api,
    IStreamService Stream,
    IStreamHub StreamHub,
    ILocalWalletSeeder Seeder,
    IPrefsStore Prefs,
    IPrefsSyncService PrefsSync,
    IAccountBootstrapService Bootstrap,
    IProductSessionStore ProductSessions,
    TimeProvider Time);
