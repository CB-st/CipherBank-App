// <copyright file="HomeSectionToggle.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
