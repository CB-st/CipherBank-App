// <copyright file="ICoraLineProvider.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Cora;

/// <summary>Resolves localizable Cora copy for a screen key.</summary>
public interface ICoraLineProvider
{
    string GetLine(string screen);
}
