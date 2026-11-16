using System.Reflection;

using Avalonia.Input;

using ClockShot.App.Input;

namespace ClockShot.App.Tests.Input;

public sealed class ShortcutsTests
{
    [Fact]
    public void EveryKeyIsInTheHelpList()
    {
        // Arrange
        var keys = typeof(Shortcuts)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(KeyGesture))
            .Select(property => (KeyGesture)property.GetValue(obj: null)!)
            .ToList();

        // Act
        var listed = Shortcuts.All.Select(shortcut => shortcut.Gesture).ToHashSet();

        // Assert
        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.Contains(key, listed));
    }
}
