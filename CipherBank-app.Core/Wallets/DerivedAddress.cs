// <copyright file="DerivedAddress.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>HD-derived address result.</summary>
public sealed record DerivedAddress(string Address, string Path, int AccountIndex);
