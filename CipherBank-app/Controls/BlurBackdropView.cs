// <copyright file="BlurBackdropView.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Controls;

/// <summary>
/// Platform-owned glass backdrop. Apple handlers render native blur; other supported
/// platforms register an explicit simulated material handler.
/// </summary>
public class BlurBackdropView : View
{
    public static readonly BindableProperty UseDarkMaterialProperty = BindableProperty.Create(
        nameof(UseDarkMaterial),
        typeof(bool),
        typeof(BlurBackdropView),
        false);

    public bool UseDarkMaterial
    {
        get => (bool)GetValue(UseDarkMaterialProperty);
        set => SetValue(UseDarkMaterialProperty, value);
    }
}
