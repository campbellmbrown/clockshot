namespace ClockShot.App.Tests;

public sealed class AppPathsTests
{
    [Fact]
    public void KeepsTheDataAndTheLogsUnderTheRoot()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), "data");

        // Act
        var paths = new AppPaths(root);

        // Assert
        Assert.Equal(root, paths.Data.Root);
        Assert.Equal(Path.Combine(root, "logs"), paths.Logs);
    }

    [Fact]
    public void DefaultsToTheAppsFolderInLocalAppData()
    {
        // Arrange
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Act
        var root = AppPaths.Default.Root;

        // Assert
        Assert.Equal(Path.Combine(localAppData, "ClockShot"), root);
    }
}
