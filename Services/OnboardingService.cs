using System;
using System.IO;
using BereitschaftsPlaner.Avalonia.Models;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for managing first-time user onboarding experience
/// </summary>
public class OnboardingService
{
    private readonly string _settingsPath;
    private OnboardingState _state = new();

    public OnboardingService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BereitschaftsPlaner"
        );

        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "onboarding.json");

        LoadState();
    }

    /// <summary>
    /// Check if this is the first time the app is launched
    /// </summary>
    public bool IsFirstLaunch()
    {
        return !_state.HasCompletedWelcome;
    }

    /// <summary>
    /// Check if specific feature tour should be shown
    /// </summary>
    public bool ShouldShowFeatureTour(string featureName)
    {
        return !_state.CompletedTours.Contains(featureName);
    }

    /// <summary>
    /// Mark welcome wizard as completed
    /// </summary>
    public void CompleteWelcome()
    {
        _state.HasCompletedWelcome = true;
        _state.WelcomeCompletedAt = DateTime.Now;
        SaveState();
        Log.Information("Welcome wizard completed");
    }

    /// <summary>
    /// Mark feature tour as completed
    /// </summary>
    public void CompleteFeatureTour(string featureName)
    {
        if (!_state.CompletedTours.Contains(featureName))
        {
            _state.CompletedTours.Add(featureName);
            SaveState();
            Log.Information("Feature tour completed: {Feature}", featureName);
        }
    }

    /// <summary>
    /// Reset onboarding state (for testing or re-onboarding)
    /// </summary>
    public void ResetOnboarding()
    {
        _state = new OnboardingState();
        SaveState();
        Log.Information("Onboarding state reset");
    }

    /// <summary>
    /// Increment help views counter
    /// </summary>
    public void IncrementHelpViews()
    {
        _state.HelpViewsCount++;
        SaveState();
    }

    /// <summary>
    /// Get onboarding statistics
    /// </summary>
    public OnboardingStats GetStats()
    {
        return new OnboardingStats
        {
            HasCompletedWelcome = _state.HasCompletedWelcome,
            WelcomeCompletedAt = _state.WelcomeCompletedAt,
            CompletedToursCount = _state.CompletedTours.Count,
            HelpViewsCount = _state.HelpViewsCount
        };
    }

    private void LoadState()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _state = System.Text.Json.JsonSerializer.Deserialize<OnboardingState>(json) ?? new OnboardingState();
            }
            else
            {
                _state = new OnboardingState();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load onboarding state");
            _state = new OnboardingState();
        }
    }

    private void SaveState()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_state, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save onboarding state");
        }
    }
}

/// <summary>
/// Onboarding state model
/// </summary>
public class OnboardingState
{
    public bool HasCompletedWelcome { get; set; } = false;
    public DateTime? WelcomeCompletedAt { get; set; }
    public System.Collections.Generic.List<string> CompletedTours { get; set; } = new();
    public int HelpViewsCount { get; set; } = 0;
}

/// <summary>
/// Onboarding statistics
/// </summary>
public class OnboardingStats
{
    public bool HasCompletedWelcome { get; set; }
    public DateTime? WelcomeCompletedAt { get; set; }
    public int CompletedToursCount { get; set; }
    public int HelpViewsCount { get; set; }
}
