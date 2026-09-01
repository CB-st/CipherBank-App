// <copyright file="FundingMixBar.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Controls;

/// <summary>Horizontal stacked funding mix bar (Cora FundingMixBar).</summary>
public sealed class FundingMixBar : ContentView
{
    public static readonly BindableProperty SourcesProperty = BindableProperty.Create(
        nameof(Sources),
        typeof(IList<MixSource>),
        typeof(FundingMixBar),
        propertyChanged: OnChanged);

    public static readonly BindableProperty TotalProperty = BindableProperty.Create(
        nameof(Total),
        typeof(double),
        typeof(FundingMixBar),
        0.0,
        propertyChanged: OnChanged);

    private readonly VerticalStackLayout _root = new() { Spacing = 8 };
    private readonly Grid _bar = new() { HeightRequest = 14, ColumnSpacing = 2 };

    public FundingMixBar()
    {
        Content = _root;
        _root.Children.Add(_bar);
    }

    public IList<MixSource>? Sources
    {
        get => (IList<MixSource>?)GetValue(SourcesProperty);
        set => SetValue(SourcesProperty, value);
    }

    public double Total
    {
        get => (double)GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
        => ((FundingMixBar)bindable).Rebuild();

    private void Rebuild()
    {
        _bar.Children.Clear();
        _bar.ColumnDefinitions.Clear();
        _root.Children.Clear();
        _root.Children.Add(_bar);

        if (Sources is null || Sources.Count == 0 || Total <= 0)
        {
            return;
        }

        int col = 0;
        foreach (var src in Sources)
        {
            double share = Math.Clamp(src.Value / Total, 0, 1);
            if (share <= 0)
            {
                continue;
            }

            _bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(share, GridUnitType.Star) });
            BoxView segment = new BoxView
            {
                Color = src.Color,
                CornerRadius = 4,
            };
            _bar.Add(segment, col, 0);
            col++;
        }

        VerticalStackLayout legend = new VerticalStackLayout { Spacing = 4 };
        foreach (var src in Sources)
        {
            double pct = (src.Value / Total) * 100;
            legend.Children.Add(new Label
            {
                Text = $"{src.Asset}: {src.Value:0.##} ({pct:0.#}%)",
                FontSize = 13,
            });
        }

        _root.Children.Add(legend);
    }
}
