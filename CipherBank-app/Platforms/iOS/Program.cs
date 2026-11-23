// <copyright file="Program.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using UIKit;

namespace CipherBank_app;

/// <summary>
/// iOS application entry point.
/// </summary>
public static class Program
{
    // This is the main entry point of the application.
    // To use a different application delegate, specify its type here.
    public static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}
