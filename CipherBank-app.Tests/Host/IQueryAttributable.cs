// <copyright file="IQueryAttributable.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ViewModels;

/// <summary>
/// Test stand-in for the MAUI query-attribute contract.
/// PurchaseViewModel is compiled into this suite without the MAUI workload, and this
/// interface occupies the same namespace so that source binds here.
/// </summary>
public interface IQueryAttributable
{
    /// <summary>Applies navigation query values.</summary>
    /// <param name="query">Query values supplied by the navigation host.</param>
    void ApplyQueryAttributes(IDictionary<string, object> query);
}
