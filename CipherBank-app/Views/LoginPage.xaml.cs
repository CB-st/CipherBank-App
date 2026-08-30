// <copyright file="LoginPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;
#if DEBUG
using Microsoft.Maui.Controls.Shapes;
#endif

namespace CipherBank_app.Views;

/// <summary>
/// Code-behind for the Login page.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

#if DEBUG
        AddDeveloperControls();
#endif
    }

#if DEBUG
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
        // Environment badge overlay, shown only in test/non-production environments.
        var badgeLabel = new Label
        {
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
        };
        badgeLabel.SetBinding(Label.TextProperty, new Binding(nameof(LoginViewModel.EnvironmentBadge)));

        var badge = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(10, 5),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 50, 0, 0),
            StrokeShape = new RoundRectangle { CornerRadius = 5 },
            Content = badgeLabel,
        };
        ApplyThemeColor(badge, BackgroundColorProperty, "EnvironmentBadge", "EnvironmentBadgeDark");
        badge.SetBinding(IsVisibleProperty, new Binding(nameof(LoginViewModel.IsTestEnvironment)));
        RootGrid.Children.Add(badge);

        // Quick-login button for filling mock credentials.
        var testCredentials = new Button
        {
            Text = "Use Test Credentials",
            AutomationId = "TestCredentialsButton",
            Style = GetStyle("SecondaryButton"),
            HorizontalOptions = LayoutOptions.Fill,
            Margin = new Thickness(0, 0, 0, 20),
            Padding = new Thickness(0, 10),
            Command = _viewModel.UseTestCredentialsCommand,
        };
        testCredentials.SetBinding(IsVisibleProperty, new Binding(nameof(LoginViewModel.IsTestEnvironment)));

        var insertIndex = LoginFormLayout.Children.IndexOf(LoginButton) + 1;
        LoginFormLayout.Children.Insert(insertIndex, testCredentials);
    }
#endif

    private void OnUsernameCompleted(object? sender, EventArgs e)
    {
        // Move focus to password field when Enter is pressed on username
        PasswordEntry.Focus();
    }

    private void OnPasswordCompleted(object? sender, EventArgs e)
    {
        // Submit login when Enter is pressed on password field
        if (_viewModel.SignInCommand.CanExecute(null))
        {
            _viewModel.SignInCommand.Execute(null);
        }
    }
}
