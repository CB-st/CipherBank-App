// <copyright file="SettingsPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;
#if DEBUG
using Microsoft.Maui.Controls.Shapes;
#endif

namespace CipherBank_app.Views;

/// <summary>
/// Code-behind for the Settings page.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

#if DEBUG
        AddDeveloperControls();
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

#if DEBUG
    private static Border BuildDeveloperCard()
    {
        var headerLabel = new Label
        {
            Text = "Developer Mode Active",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
        };
        ApplyThemeColor(headerLabel, Label.TextColorProperty, "EnvironmentBadge", "EnvironmentBadgeDark");
        Grid.SetColumn(headerLabel, 0);

        var devBadge = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(6, 2),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            Content = new Label
            {
                Text = "DEV",
                TextColor = Colors.White,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
            },
        };
        ApplyThemeColor(devBadge, BackgroundColorProperty, "EnvironmentBadge", "EnvironmentBadgeDark");
        Grid.SetColumn(devBadge, 1);

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            Children = { headerLabel, devBadge },
        };

        var environmentLabel = new Label { Text = "Environment", FontSize = 12, Style = GetStyle("SecondaryText") };
        var environmentPicker = new Picker { Title = "Select environment" };
        environmentPicker.SetBinding(Picker.ItemsSourceProperty, new Binding(nameof(SettingsViewModel.Environments)));
        environmentPicker.SetBinding(Picker.SelectedItemProperty, new Binding(nameof(SettingsViewModel.SelectedEnvironment), BindingMode.TwoWay));

        var environmentSection = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { environmentLabel, environmentPicker },
        };

        var mockLabel = new Label { Text = "Use Mock Services", VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(mockLabel, 0);
        var mockSwitch = new Switch();
        mockSwitch.SetBinding(Switch.IsToggledProperty, new Binding(nameof(SettingsViewModel.UseMockServices), BindingMode.TwoWay));
        Grid.SetColumn(mockSwitch, 1);

        var mockGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            Children = { mockLabel, mockSwitch },
        };

        var noteLabel = new Label
        {
            Text = "Note: Changing environment will clear your authentication",
            FontSize = 11,
            Style = GetStyle("SecondaryText"),
            HorizontalTextAlignment = TextAlignment.Center,
        };

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(16),
            Stroke = Brush.Transparent,
            Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 2), Radius = 4, Opacity = 0.15f },
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children = { headerGrid, environmentSection, mockGrid, noteLabel },
            },
        };
        ApplyThemeColor(card, BackgroundColorProperty, "DevModeBackground", "DevModeBackgroundDark");
        card.SetBinding(IsVisibleProperty, new Binding(nameof(SettingsViewModel.DeveloperModeEnabled)));

        return card;
    }

    private static void ApplyThemeColor(VisualElement element, BindableProperty property, string lightKey, string darkKey) =>
        element.SetAppThemeColor(property, GetColor(lightKey), GetColor(darkKey));

    private static Color GetColor(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Colors.Transparent;

    private static Style? GetStyle(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true ? value as Style : null;

    // Developer-only affordances are built in code so they are not compiled into Release builds.
    private void AddDeveloperControls()
    {
        // Tap the version label three times to toggle developer mode.
        VersionLabel.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = _viewModel.TapVersionCommand,
        });

        // The developer card is inserted right after the API configuration card and is
        // only visible once developer mode has been enabled.
        var insertIndex = SettingsLayout.Children.IndexOf(ApiSettingsCard) + 1;
        SettingsLayout.Children.Insert(insertIndex, BuildDeveloperCard());
    }
#endif
}
