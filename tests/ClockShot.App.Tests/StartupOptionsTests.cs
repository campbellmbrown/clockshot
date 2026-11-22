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
        var options = StartupOptions.Parse(["--data-folder", dataFolder, "--startup"]);

        // Assert
        Assert.Equal(new StartupOptions(dataFolder, StartedWithWindows: true), options);
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

    [Fact]
    public void StartsTheExecutableWithWindows()
    {
        // Arrange
        var folder = Path.Combine(Path.GetTempPath(), "Apps");
        var executable = Path.Combine(folder, "ClockShot.exe");

        // Act
        var command = StartupOptions.StartWithWindowsCommand(executable, folder, AppPaths.Default);

        // Assert
        Assert.Equal($"\"{executable}\" --startup", command);
    }

    [Fact]
    public void StartsTheDllThroughDotnetWhenRunThatWay()
    {
        // Arrange
        var dotnet = Path.Combine(Path.GetTempPath(), "dotnet", "dotnet.exe");
        var folder = Path.Combine(Path.GetTempPath(), "Source");

        // Act
        var command = StartupOptions.StartWithWindowsCommand(dotnet, folder, AppPaths.Default);

        // Assert
        Assert.Equal($"\"{dotnet}\" \"{Path.Combine(folder, "ClockShot.dll")}\" --startup", command);
    }

    [Fact]
    public void StartsTheExecutableInTheAppFolderWhenTheProcessPathIsUnknown()
    {
        // Arrange
        var folder = Path.Combine(Path.GetTempPath(), "Apps");

        // Act
        var command = StartupOptions.StartWithWindowsCommand(processPath: null, folder, AppPaths.Default);

        // Assert
        Assert.Equal($"\"{Path.Combine(folder, "ClockShot.exe")}\" --startup", command);
    }

    [Fact]
    public void KeepsADataFolderThatIsNotTheDefault()
    {
        // Arrange
        var folder = Path.Combine(Path.GetTempPath(), "Apps");
        var executable = Path.Combine(folder, "ClockShot.exe");
        var dataFolder = Path.Combine(Path.GetTempPath(), "Data");

        // Act
        var command = StartupOptions.StartWithWindowsCommand(executable, folder, new AppPaths(dataFolder));

        // Assert
        Assert.Equal($"\"{executable}\" --data-folder \"{dataFolder}\" --startup", command);
    }

    [Fact]
    public void DoublesATrailingBackslashSoItDoesNotEscapeTheQuote()
    {
        // Arrange
        var folder = Path.Combine(Path.GetTempPath(), "Apps");
        var executable = Path.Combine(folder, "ClockShot.exe");
        var driveRoot = Path.GetPathRoot(Path.GetTempPath())!;

        // Act
        var command = StartupOptions.StartWithWindowsCommand(executable, folder, new AppPaths(driveRoot));

        // Assert
        Assert.Equal($"\"{executable}\" --data-folder \"{driveRoot}\\\" --startup", command);
    }
}
