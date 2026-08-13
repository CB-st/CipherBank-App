// <copyright file="Strings.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Resources;

namespace CipherBank_app.Resources;

/// <summary>Strongly-typed access to Core user-facing strings (S4055 ResourceManager).</summary>
public static class Strings
{
    private static readonly ResourceManager Manager =
        new("CipherBank_app.Resources.Strings", typeof(Strings).Assembly);

    public static string AchEnterPayeeName => Get(nameof(AchEnterPayeeName));

    public static string AchEnterAccountHolderName => Get(nameof(AchEnterAccountHolderName));

    public static string AchEnterBankName => Get(nameof(AchEnterBankName));

    public static string AchEnterValidAccountNumber => Get(nameof(AchEnterValidAccountNumber));

    public static string AchAccountTypeMustBeCheckingOrSavings => Get(nameof(AchAccountTypeMustBeCheckingOrSavings));

    public static string PinChangeSuccess => Get(nameof(PinChangeSuccess));

    public static string PinChangeMismatch => Get(nameof(PinChangeMismatch));

    public static string PinChangeSameAsCurrent => Get(nameof(PinChangeSameAsCurrent));

    public static string PinChangeWrongCurrentPin => Get(nameof(PinChangeWrongCurrentPin));

    public static string PinChangeLockedOut => Get(nameof(PinChangeLockedOut));

    public static string PinChangeVaultNotReady => Get(nameof(PinChangeVaultNotReady));

    public static string AchRoutingNumberMustBeDigits(int digitCount)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(AchRoutingNumberMustBeDigits)), digitCount);

    public static string AchMemoMustBeMaxLength(int maxLength)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(AchMemoMustBeMaxLength)), maxLength);

    public static string PinChangeTooShort(int minLength)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(PinChangeTooShort)), minLength);

    private static string Get(string name)
        => Manager.GetString(name, CultureInfo.CurrentUICulture)
           ?? Manager.GetString(name, CultureInfo.InvariantCulture)
           ?? name;
}
