namespace ClockShot.App.Tests;

public sealed class StartupOptionsTests
{
    [Fact]
    public void NoArgumentsMeanNoOptions()
    {
        // Act
        var options = StartupOptions.Parse([]);

        // Assert
        Assert.Equal(StartupOptions.None, options);
        Assert.Equal(AppPaths.Default, options.Paths);
    }

    [Fact]
    public void ReadsEveryOption()
    {
        // Arrange
        var dataFolder = Path.Combine(Path.GetTempPath(), "Data");

        // Act
        var options = StartupOptions.Parse(["--data-folder", dataFolder]);

        // Assert
        Assert.Equal(new StartupOptions(dataFolder), options);
        Assert.Equal(new AppPaths(dataFolder), options.Paths);
    }

    [Fact]
    public void MakesRelativePathsFull()
    {
        // Act
        var options = StartupOptions.Parse(["--data-folder", "data"]);

        // Assert
        Assert.Equal(Path.GetFullPath("data"), options.DataFolder);
    }

    [Fact]
    public void RejectsAnUnknownOption()
    {
        // Act
        var error = Record.Exception(() => StartupOptions.Parse(["--colour", "blue"]));

        // Assert
        Assert.IsType<ArgumentException>(error);
        Assert.Equal("Unknown option --colour.", error.Message);
    }

    [Fact]
    public void RejectsAnUnknownOptionWithNothingAfterIt()
    {
        // Act
        var error = Record.Exception(() => StartupOptions.Parse(["--colour"]));

        // Assert
        Assert.IsType<ArgumentException>(error);
        Assert.Equal("Unknown option --colour.", error.Message);
    }

    [Fact]
    public void RejectsAnOptionWithoutItsValue()
    {
        // Act
        var error = Record.Exception(() => StartupOptions.Parse(["--data-folder"]));

        // Assert
        Assert.IsType<ArgumentException>(error);
        Assert.Equal("--data-folder needs a value.", error.Message);
    }
}
