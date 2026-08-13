// <copyright file="EmvStageItem.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherBank_app.ViewModels;

/// <summary>EMV stage chip for PosLab UI.</summary>
public partial class EmvStageItem : ObservableObject
{
    public EmvStageItem(string text, bool done)
    {
        Text = text;
        Done = done;
    }

    public string Text { get; }

    [ObservableProperty]
    private bool done;

    public string StatusLabel => Done ? "done" : "…";

    partial void OnDoneChanged(bool value) => OnPropertyChanged(nameof(StatusLabel));
}
