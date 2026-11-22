using System.Text;

using Avalonia.Platform.Storage;

namespace ClockShot.App.Services;

internal sealed class FileSaveService(MainWindowHost host) : IFileSaveService
{
    public async Task SaveTextAsync(string title, string suggestedName, string fileTypeName, string extension, string content)
    {
        var storage = host.Window.StorageProvider;
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(fileTypeName) { Patterns = [$"*.{extension}"] }],
            ShowOverwritePrompt = true,
        });
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();

        // Empties a file being replaced, in case it was opened without being emptied.
        stream.SetLength(0);

        // UTF-8 with a byte order mark, so spreadsheets read accented names correctly.
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteAsync(content);
    }
}
