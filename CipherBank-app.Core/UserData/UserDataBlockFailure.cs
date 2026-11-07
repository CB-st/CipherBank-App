// <copyright file="UserDataBlockFailure.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Identity of a pack block that could not be opened. Use: High (GRAB apply). Scope: userdata restore.
/// </summary>
/// <param name="Id">Block id from the wire envelope.</param>
/// <param name="Type">Block type, so the caller can mark it dirty for rewrite.</param>
/// <param name="Reason">Why the block failed to open.</param>
public sealed record UserDataBlockFailure(string Id, string Type, UserDataBlockFailureReason Reason);
