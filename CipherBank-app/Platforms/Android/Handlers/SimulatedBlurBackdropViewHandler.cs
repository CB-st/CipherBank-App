// <copyright file="SimulatedBlurBackdropViewHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Android.Graphics.Drawables;
using CipherBank_app.Controls;
using Microsoft.Maui.Handlers;
using Color = Android.Graphics.Color;
using View = Android.Views.View;

namespace CipherBank_app.Handlers;

/// <summary>Renders the non-native glass fallback on Android.</summary>
public sealed class SimulatedBlurBackdropViewHandler() :
    ViewHandler<BlurBackdropView, View>(Mapper)
{
    public static readonly IPropertyMapper<BlurBackdropView, SimulatedBlurBackdropViewHandler> Mapper =
        new PropertyMapper<BlurBackdropView, SimulatedBlurBackdropViewHandler>(ViewMapper)
        {
            [nameof(BlurBackdropView.UseDarkMaterial)] = MapMaterial,
        };

    protected override View CreatePlatformView() =>
        new(Context);

    private static void MapMaterial(
        SimulatedBlurBackdropViewHandler handler,
        BlurBackdropView view)
    {
        int alpha = view.UseDarkMaterial ? 204 : 235;
        handler.PlatformView.Background = new ColorDrawable(
            Color.Argb(alpha, 24, 24, 30));
    }
}
