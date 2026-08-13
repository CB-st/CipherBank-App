// <copyright file="BlurBackdropViewHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Controls;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CipherBank_app.Handlers;

/// <summary>
/// Renders <see cref="BlurBackdropView"/> as a native UIVisualEffectView so glass cards
/// refract whatever is behind them.
/// </summary>
public class BlurBackdropViewHandler : ViewHandler<BlurBackdropView, UIVisualEffectView>
{
    public static readonly IPropertyMapper<BlurBackdropView, BlurBackdropViewHandler> BlurMapper =
        new PropertyMapper<BlurBackdropView, BlurBackdropViewHandler>(ViewMapper)
        {
            [nameof(BlurBackdropView.UseDarkMaterial)] = MapUseDarkMaterial,
        };

    public BlurBackdropViewHandler()
        : base(BlurMapper)
    {
    }

    protected override UIVisualEffectView CreatePlatformView() => new();

    private static void MapUseDarkMaterial(BlurBackdropViewHandler handler, BlurBackdropView view) =>
        handler.PlatformView.Effect = UIBlurEffect.FromStyle(
            view.UseDarkMaterial
                ? UIBlurEffectStyle.SystemUltraThinMaterialDark
                : UIBlurEffectStyle.SystemUltraThinMaterialLight);
}
