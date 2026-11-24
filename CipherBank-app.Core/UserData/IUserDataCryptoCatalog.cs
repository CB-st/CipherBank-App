// <copyright file="IUserDataCryptoCatalog.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Registry of userdata crypto suites with one active selection.</summary>
public interface IUserDataCryptoCatalog
{
    string ActiveSuiteId { get; }

    UserDataCryptoSuite Active { get; }

    /// <summary>
    /// Selects a registered suite by id. Use: Low (lab / config). Scope: process catalog.
    /// </summary>
    void SetActive(string suiteId);

    /// <summary>
    /// Returns a registered suite or throws. Use: Low. Scope: process catalog.
    /// </summary>
    UserDataCryptoSuite GetSuite(string suiteId);
}
