using System.IO.Abstractions;
using System.Text.Json;

using ClockShot.Core.Settings;

using Microsoft.Extensions.Logging;

namespace ClockShot.Data.Settings;

/// <summary>
/// Keeps settings in a JSON file.
/// Missing values get their defaults, so older files keep working.
/// Values out of range are brought into range by <see cref="AppSettings.Normalized"/>.
/// A file that can't be read is renamed to <c>.bad</c>, and defaults are used.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IFileSystem _fileSystem;
    private readonly string _folder;
    private readonly string _filePath;
    private readonly ILogger<JsonSettingsStore> _logger;
    private readonly Lock _saveLock = new();
    private volatile AppSettings _current;

    public JsonSettingsStore(IFileSystem fileSystem, DataPaths paths, ILogger<JsonSettingsStore> logger)
    {
        _fileSystem = fileSystem;
        _folder = paths.Root;
        _filePath = paths.Settings;
        _logger = logger;
        _current = Load();
    }

    public AppSettings Current => _current;

    public event Action<AppSettings>? Changed;

    public void Save(AppSettings settings)
    {
        settings = settings.Normalized();
        lock (_saveLock)
        {
            _fileSystem.Directory.CreateDirectory(_folder);
            var temporaryPath = _filePath + ".tmp";
            _fileSystem.File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            _fileSystem.File.Move(temporaryPath, _filePath, overwrite: true);
            _current = settings;
        }

        Changed?.Invoke(settings);
    }

    private AppSettings Load()
    {
        if (!_fileSystem.File.Exists(_filePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = _fileSystem.File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)?.Normalized() ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Settings file {Path} could not be read. Defaults are used.", _filePath);
            SetAside();
            return new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Settings file {Path} could not be opened. Defaults are used.", _filePath);
            return new AppSettings();
        }
    }

    /// <summary>
    /// Renames a file that can't be read, so the user can still find it.
    /// </summary>
    private void SetAside()
    {
        try
        {
            _fileSystem.File.Move(_filePath, _filePath + ".bad", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Settings file {Path} could not be renamed to .bad.", _filePath);
        }
    }
}
