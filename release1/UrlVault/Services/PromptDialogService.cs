using System.Windows;
using System.Windows.Controls;

namespace UrlVault.Services;

public static class PromptDialogService
{
    public static string? ShowTextInput(string title, string label, string initialValue = "")
    {
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

        var window = new Window
        {
            Title = title,
            Width = 420,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.SingleBorderWindow,
            Owner = owner,
            Background = System.Windows.Media.Brushes.White
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
            FontWeight = FontWeights.SemiBold
        };

        var input = new TextBox
        {
            Text = initialValue ?? string.Empty,
            MinWidth = 260,
            Padding = new Thickness(8, 5, 8, 5)
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
            IsDefault = true
        };
        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 90,
            IsCancel = true
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
