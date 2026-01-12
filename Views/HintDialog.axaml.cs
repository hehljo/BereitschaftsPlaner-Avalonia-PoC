using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.IO;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class HintDialog : Window
{
    private readonly TextBlock _titleText;
    private readonly TextBlock _messageText;
    private readonly Border _screenshotBorder;
    private readonly Image _screenshotImage;

    public HintDialog(string title, string message, string? screenshotPath = null)
    {
        InitializeComponent();

        _titleText = this.FindControl<TextBlock>("TitleText") ?? throw new InvalidOperationException("TitleText not found");
        _messageText = this.FindControl<TextBlock>("MessageText") ?? throw new InvalidOperationException("MessageText not found");
        _screenshotBorder = this.FindControl<Border>("ScreenshotBorder") ?? throw new InvalidOperationException("ScreenshotBorder not found");
        _screenshotImage = this.FindControl<Image>("ScreenshotImage") ?? throw new InvalidOperationException("ScreenshotImage not found");

        _titleText.Text = title;
        _messageText.Text = message;

        if (!string.IsNullOrEmpty(screenshotPath))
        {
            LoadScreenshot(screenshotPath);
        }
    }

    private void LoadScreenshot(string path)
    {
        try
        {
            // Try loading from Assets first (embedded resource)
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri($"avares://BereitschaftsPlaner.Avalonia/{path}");
                var bitmap = new Bitmap(AssetLoader.Open(uri));
                _screenshotImage.Source = bitmap;
                _screenshotBorder.IsVisible = true;
                Serilog.Log.Debug($"Loaded screenshot from assets: {path}");
            }
            // Try loading from file system
            else if (File.Exists(path))
            {
                var bitmap = new Bitmap(path);
                _screenshotImage.Source = bitmap;
                _screenshotBorder.IsVisible = true;
                Serilog.Log.Debug($"Loaded screenshot from file: {path}");
            }
            else
            {
                Serilog.Log.Warning($"Screenshot not found: {path}");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, $"Failed to load screenshot: {path}");
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
