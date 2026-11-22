namespace ClockShot.Platform.Windows.Tests;

/// <summary>
/// Arranges windows of the test process on the real desktop and lists what's on top of one.
/// </summary>
public sealed class WindowsOnTopTests
{
    // Where the test windows go on the main screen.
    private const int Left = 100;
    private const int Top = 100;
    private const int Size = 200;

    private static readonly string TestProcessName = Path.GetFileName(Environment.ProcessPath!);

    private readonly WindowsWindowCapturer _capturer = new();

    [Fact]
    public void ListsAnOverlappingWindowInFront()
    {
        // Arrange
        using var behind = TestWindow.Behind(Left, Top, Size);
        using var inFront = TestWindow.InFront(Left + (Size / 2), Top, Size);

        // Act
        var names = _capturer.ProcessNamesOnTopOf(behind.Handle);

        // Assert
        Assert.Contains(TestProcessName, names);
    }

    [Fact]
    public void LeavesOutAWindowInFrontThatDoesNotOverlap()
    {
        // Arrange
        using var behind = TestWindow.Behind(Left, Top, Size);
        using var inFront = TestWindow.InFront(Left + Size, Top, Size);

        // Act
        var names = _capturer.ProcessNamesOnTopOf(behind.Handle);

        // Assert
        Assert.DoesNotContain(TestProcessName, names);
    }

    [Fact]
    public void LeavesOutAnOverlappingWindowBehind()
    {
        // Arrange
        using var behind = TestWindow.Behind(Left, Top, Size);
        using var inFront = TestWindow.InFront(Left + (Size / 2), Top, Size);

        // Act
        var names = _capturer.ProcessNamesOnTopOf(inFront.Handle);

        // Assert
        Assert.DoesNotContain(TestProcessName, names);
    }

    [Fact]
    public void LeavesOutAHiddenWindowInFront()
    {
        // Arrange
        using var behind = TestWindow.Behind(Left, Top, Size);
        using var hidden = TestWindow.HiddenInFront(Left, Top, Size);

        // Act
        var names = _capturer.ProcessNamesOnTopOf(behind.Handle);

        // Assert
        Assert.DoesNotContain(TestProcessName, names);
    }
}
