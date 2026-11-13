using System.IO.Abstractions.TestingHelpers;

using ClockShot.Core.Settings;
using ClockShot.Data.Settings;

using Microsoft.Extensions.Logging.Abstractions;

namespace ClockShot.Data.Tests;

public sealed class JsonSettingsStoreTests
{
    private static readonly DataPaths Paths = new("data");
    private static readonly string SettingsPath = Paths.Settings;

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void MissingFileGivesDefaults()
    {
        // Act
        var store = CreateStore();

        // Assert
        Assert.Equal(new AppSettings(), store.Current);
    }

    [Fact]
    public void SavedSettingsAreReadBackAndAnnounced()
    {
        // Arrange
        var store = CreateStore();
        AppSettings? announced = null;
        store.Changed += settings => announced = settings;
        var changed = store.Current with { General = store.Current.General with { CloseToTray = false } };

        // Act
        store.Save(changed);

        // Assert
        Assert.Equal(changed, announced);
        Assert.False(CreateStore().Current.General.CloseToTray);
    }

    [Fact]
    public void ValuesMissingFromFileGetDefaults()
    {
        // Arrange
        _fileSystem.AddFile(SettingsPath, new MockFileData("""{ "General": {} }"""));

        // Act
        var settings = CreateStore().Current;

        // Assert
        Assert.Equal(new GeneralSettings(), settings.General);
    }

    [Fact]
    public void NullSectionGetsDefaults()
    {
        // Arrange
        _fileSystem.AddFile(SettingsPath, new MockFileData("""{ "General": null }"""));

        // Act
        var settings = CreateStore().Current;

        // Assert
        Assert.Equal(new GeneralSettings(), settings.General);
    }

    [Fact]
    public void OutOfRangeValuesAreBroughtIntoRangeOnLoadAndSave()
    {
        // Arrange
        _fileSystem.AddFile(SettingsPath, new MockFileData("""{ "Tracking": { "AwayAfterMinutes": 0 } }"""));

        // Act
        var store = CreateStore();
        var loaded = store.Current.Tracking.AwayAfterMinutes;
        store.Save(store.Current with { Tracking = store.Current.Tracking with { AwayAfterMinutes = 999 } });

        // Assert
        Assert.Equal(SettingLimits.AwayAfterMinutes.Min, loaded);
        Assert.Equal(SettingLimits.AwayAfterMinutes.Max, store.Current.Tracking.AwayAfterMinutes);
    }

    [Fact]
    public void UnreadableFileIsSetAsideAndDefaultsUsed()
    {
        // Arrange
        _fileSystem.AddFile(SettingsPath, new MockFileData("{ not json"));

        // Act
        var settings = CreateStore().Current;

        // Assert
        Assert.Equal(new AppSettings(), settings);
        Assert.True(_fileSystem.File.Exists(SettingsPath + ".bad"));
    }

    [Fact]
    public void UnreadableFileThatCannotBeSetAsideStillGivesDefaults()
    {
        // Arrange
        _fileSystem.AddFile(SettingsPath, new MockFileData("{ not json"));
        _fileSystem.AddFile(SettingsPath + ".bad", new MockFileData("") { AllowedFileShare = FileShare.None });

        // Act
        var settings = CreateStore().Current;

        // Assert
        Assert.Equal(new AppSettings(), settings);
    }

    [Fact]
    public void FileThatCannotBeOpenedGivesDefaults()
    {
        // Arrange
        _fileSystem.AddFile(SettingsPath, new MockFileData("{}") { AllowedFileShare = FileShare.None });

        // Act
        var settings = CreateStore().Current;

        // Assert
        Assert.Equal(new AppSettings(), settings);
    }

    private JsonSettingsStore CreateStore() => new(_fileSystem, Paths, NullLogger<JsonSettingsStore>.Instance);
}
