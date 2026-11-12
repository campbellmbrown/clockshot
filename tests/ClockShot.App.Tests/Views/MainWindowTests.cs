using Avalonia.Controls;

using ClockShot.App.Views;

namespace ClockShot.App.Tests.Views;

public sealed class MainWindowTests
{
    [Fact]
    public void ClosingTheWindowHidesItWhenCloseToTrayIsOn()
    {
        // Act
        var action = MainWindow.ActionForClose(WindowCloseReason.WindowClosing, isProgrammatic: false, closeToTray: true);

        // Assert
        Assert.Equal(CloseAction.HideToTray, action);
    }

    [Fact]
    public void ClosingTheWindowExitsTheAppWhenCloseToTrayIsOff()
    {
        // Act
        var action = MainWindow.ActionForClose(WindowCloseReason.WindowClosing, isProgrammatic: false, closeToTray: false);

        // Assert
        Assert.Equal(CloseAction.ExitApp, action);
    }

    [Theory]
    [InlineData(WindowCloseReason.WindowClosing, true)]
    [InlineData(WindowCloseReason.ApplicationShutdown, false)]
    [InlineData(WindowCloseReason.OSShutdown, false)]
    public void ACloseTheUserDidNotAskForJustCloses(WindowCloseReason reason, bool isProgrammatic)
    {
        // Act
        var action = MainWindow.ActionForClose(reason, isProgrammatic, closeToTray: true);

        // Assert
        Assert.Equal(CloseAction.Close, action);
    }
}
