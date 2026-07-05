// <copyright file="BlurBackdropView.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Controls;

/// <summary>
/// Placeholder view whose platform handler (registered on iOS/Mac Catalyst only) renders a
/// native UIVisualEffectView blur. Never add this view to the tree on platforms without a
/// registered handler; <see cref="GlassCard"/> guards this with compile-time directives.
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
