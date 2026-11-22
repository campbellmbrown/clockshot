using ClockShot.Core;

using Microsoft.Win32;

namespace ClockShot.Platform.Windows.Tests;

/// <summary>
/// Each test uses its own registry key under HKEY_CURRENT_USER and deletes it afterwards.
/// The real Run key is never touched.
/// </summary>
public sealed class WindowsStartupRegistrationTests : IDisposable
{
    private const string Command = @"""C:\Apps\ClockShot.exe"" --startup";

    private readonly string _keyPath = $@"Software\{AppInfo.FilePrefix}-tests-{Guid.NewGuid():N}";
    private readonly WindowsStartupRegistration _registration;

    public WindowsStartupRegistrationTests()
    {
        _registration = new WindowsStartupRegistration(AppInfo.Name, Command, _keyPath);
    }

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);

    [Fact]
    public void EnablingAddsTheCommand()
    {
        // Act
        _registration.SetEnabled(true);

        // Assert
        Assert.True(_registration.IsEnabled);
        using var key = Registry.CurrentUser.OpenSubKey(_keyPath);
        Assert.Equal(Command, key?.GetValue(AppInfo.Name));
    }

    [Fact]
    public void DisablingRemovesTheCommand()
    {
        // Arrange
        _registration.SetEnabled(true);

        // Act
        _registration.SetEnabled(false);

        // Assert
        Assert.False(_registration.IsEnabled);
    }

    [Fact]
    public void DisablingWhenNotEnabledDoesNothing()
    {
        // Act
        _registration.SetEnabled(false);

        // Assert
        Assert.False(_registration.IsEnabled);
    }

    [Fact]
    public void ACommandForAnotherCopyIsNotEnabled()
    {
        // Arrange
        var otherCopy = new WindowsStartupRegistration(AppInfo.Name, @"""C:\Old\ClockShot.exe"" --startup", _keyPath);
        otherCopy.SetEnabled(true);

        // Act
        var isEnabled = _registration.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void TheCommandMatchesIgnoringCase()
    {
        // Arrange
        var sameCopy = new WindowsStartupRegistration(AppInfo.Name, Command.ToUpperInvariant(), _keyPath);
        sameCopy.SetEnabled(true);

        // Act
        var isEnabled = _registration.IsEnabled;

        // Assert
        Assert.True(isEnabled);
    }
}
