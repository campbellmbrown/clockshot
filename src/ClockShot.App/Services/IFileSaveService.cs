namespace ClockShot.App.Services;

/// <summary>
/// Asks where to save a file, then saves it.
/// </summary>
public interface IFileSaveService
{
    /// <summary>
    /// Shows a save dialog and writes <paramref name="content"/> to the chosen file.
    /// It does nothing if the user cancels.
    /// </summary>
    /// <param name="fileTypeName">The name the dialog shows for the file type.</param>
    /// <param name="extension">The file extension, without the dot.</param>
    Task SaveTextAsync(string title, string suggestedName, string fileTypeName, string extension, string content);
}
