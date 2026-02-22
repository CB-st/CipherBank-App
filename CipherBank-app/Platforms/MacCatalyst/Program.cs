// <copyright file="Program.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using ObjCRuntime;
using UIKit;

namespace CipherBank_app;

/// <summary>
/// MacCatalyst application entry point.
/// </summary>
public static class Program
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
