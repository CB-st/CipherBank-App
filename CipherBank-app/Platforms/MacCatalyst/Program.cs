// <copyright file="Program.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using UIKit;

namespace CipherBank_app;

/// <summary>
/// MacCatalyst application entry point.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
