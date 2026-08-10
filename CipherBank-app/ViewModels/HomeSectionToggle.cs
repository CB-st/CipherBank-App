// <copyright file="HomeSectionToggle.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherBank_app.ViewModels;

/// <summary>Home section toggle row.</summary>
public partial class HomeSectionToggle : ObservableObject
{
    public HomeSectionToggle(string key, string label, bool visible)
    {
        Key = key;
        Label = label;
        Visible = visible;
    }

    public string Key { get; }

    public string Label { get; }

    [ObservableProperty]
    private bool visible;
}
