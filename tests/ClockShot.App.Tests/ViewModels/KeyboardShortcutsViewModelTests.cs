using ClockShot.App.Input;
using ClockShot.App.ViewModels;

namespace ClockShot.App.Tests.ViewModels;

public sealed class KeyboardShortcutsViewModelTests
{
    [Fact]
    public void TheListShowsEveryShortcutOnceInOrder()
    {
        // Act
        var listed = new KeyboardShortcutsViewModel().Groups.SelectMany(group => group.Shortcuts);

        // Assert
        Assert.Equal(Shortcuts.All, listed);
    }
}
