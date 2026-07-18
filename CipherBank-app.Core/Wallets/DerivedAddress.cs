// <copyright file="DerivedAddress.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>HD-derived address result.</summary>
public sealed record DerivedAddress(string Address, string Path, int AccountIndex);
