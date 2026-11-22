using ClockShot.Core.Settings;

using Microsoft.Win32;

namespace ClockShot.Platform.Windows;

/// <summary>
/// Starts the app at sign-in through a value in the current user's "Run" registry key.
/// That needs no admin rights.
/// </summary>
/// <param name="name">The value's name, which is the app's name.</param>
/// <param name="command">The full command line to run, with the executable path in quotes.</param>
/// <param name="runKeyPath">The key under HKEY_CURRENT_USER that holds the value. Tests pass their own.</param>
public sealed class WindowsStartupRegistration(string name, string command, string runKeyPath) : IStartupRegistration
{
    /// <summary>
    /// The key Windows reads at sign-in.
    /// </summary>
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// An entry that starts another copy of the app, such as one that was moved, doesn't count.
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
            return key?.GetValue(name) is string stored && stored.Equals(command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(runKeyPath);
        if (enabled)
        {
            key.SetValue(name, command);
        }
        else
        {
            key.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}
