using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UrlVault.Services;

public static class PromptDialogService
{
    // Reads a SolidColorBrush from the active theme resource dictionary.
    // Falls back to the provided default if the key is absent.
    private static SolidColorBrush ThemeBrush(string key, Color fallback)
    {
        if (Application.Current?.Resources[key] is SolidColorBrush b)
            return b;
        return new SolidColorBrush(fallback);
    }

    public static string? ShowTextInput(string title, string label, string initialValue = "")
    {
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

        // Resolve theme colors at the moment the dialog is created
        var bgBrush        = ThemeBrush("AppBackground",    Color.FromRgb(0xF9, 0xF9, 0xF9));
        var surfaceBrush   = ThemeBrush("SurfaceBackground",Color.FromRgb(0xFF, 0xFF, 0xFF));
        var fgBrush        = ThemeBrush("ForegroundPrimary",Color.FromRgb(0x2A, 0x2F, 0x36));
        var labelBrush     = ThemeBrush("LabelForeground",  Color.FromRgb(0x3B, 0x80, 0xF7));
        var borderBrush    = ThemeBrush("BorderBrush",      Color.FromRgb(0xD0, 0xD5, 0xDD));
        var primaryBrush   = ThemeBrush("PrimaryBrush",     Color.FromRgb(0x3B, 0x80, 0xF7));
        var cancelBrush    = ThemeBrush("CancelBrush",      Color.FromRgb(0x75, 0x75, 0x75));

        var window = new Window
        {
            Title = title,
            Width = 420,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.SingleBorderWindow,
            Owner = owner,
            Background = bgBrush
        };

        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var prompt = new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 8),
            FontWeight = FontWeights.SemiBold,
            Foreground = labelBrush
        };

        var input = new TextBox
        {
            Text = initialValue ?? string.Empty,
            MinWidth = 260,
            Padding = new Thickness(8, 5, 8, 5),
            Background = surfaceBrush,
            Foreground = fgBrush,
            BorderBrush = borderBrush,
            CaretBrush = fgBrush
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 90,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true,
            Background = primaryBrush,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 90,
            IsCancel = true,
            Background = cancelBrush,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };

        okButton.Click += (_, _) => window.DialogResult = true;

        Grid.SetRow(prompt, 0);
        Grid.SetRow(input, 1);
        Grid.SetRow(buttonPanel, 3);
        root.Children.Add(prompt);
        root.Children.Add(input);
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        root.Children.Add(buttonPanel);
        window.Content = root;

        window.Loaded += (_, _) =>
        {
            input.Focus();
            input.SelectAll();
        };

        var accepted = window.ShowDialog();
        return accepted == true ? input.Text : null;
    }
}
