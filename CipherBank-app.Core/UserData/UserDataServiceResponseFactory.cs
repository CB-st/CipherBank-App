// <copyright file="UserDataServiceResponseFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;

namespace CipherBank_app.UserData;

/// <summary>Builds loopback response maps (__MESSAGE_TYPE__/__CODE__/fields).</summary>
public static class UserDataServiceResponseFactory
{
    /// <summary>
    /// Packs a typed response or ERROR map for the wire codec. Use: High (HandleRequest). Scope: service logic.
    /// </summary>
    public static Dictionary<string, string> Create(
        string responseType,
        UserDataStatusCode code,
        string? details,
        Dictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        fields["__MESSAGE_TYPE__"] = code == UserDataStatusCode.Ok ? responseType : UserDataWireNames.ErrorType;
        fields["__CODE__"] = ((int)code).ToString(CultureInfo.InvariantCulture);
        fields["__MESSAGE__"] = code.ToString();
        if (!string.IsNullOrWhiteSpace(details))
        {
            fields[UserDataWireNames.Details] = details;
        }

        return fields;
    }

    /// <summary>
    /// Builds an ERROR response map. Use: Medium (unknown request). Scope: service logic.
    /// </summary>
    public static Dictionary<string, string> Error(UserDataStatusCode code, string details)
        => Create(UserDataWireNames.ErrorType, code, details, new Dictionary<string, string>(StringComparer.Ordinal));
}
