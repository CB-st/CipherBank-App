// <copyright file="SimulatedBlurBackdropViewHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CipherBank_app.Handlers;

/// <summary>Renders the non-native glass fallback on Windows.</summary>
public sealed class SimulatedBlurBackdropViewHandler : ViewHandler<BlurBackdropView, Grid>
{
    public static readonly IPropertyMapper<BlurBackdropView, SimulatedBlurBackdropViewHandler> Mapper =
        new PropertyMapper<BlurBackdropView, SimulatedBlurBackdropViewHandler>(ViewMapper)
        {
            [nameof(BlurBackdropView.UseDarkMaterial)] = MapMaterial,
        };

    public SimulatedBlurBackdropViewHandler()
        : base(Mapper)
    {
    }

    protected override Grid CreatePlatformView() => new();

    private static void MapMaterial(
        SimulatedBlurBackdropViewHandler handler,
        BlurBackdropView view)
    {
        byte alpha = view.UseDarkMaterial ? (byte)204 : (byte)235;
        handler.PlatformView.Background = new SolidColorBrush(
            Windows.UI.Color.FromArgb(alpha, 24, 24, 30));
    }
}
