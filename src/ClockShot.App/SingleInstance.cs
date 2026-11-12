using System.Security.Cryptography;
using System.Text;

using ClockShot.Core;

namespace ClockShot.App;

/// <summary>
/// Keeps one running copy per data folder, because two would record everything twice.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    /// <summary>
    /// Enough hex digits to tell folders apart. Few enough to read in a debugger.
    /// </summary>
    private const int HashDigits = 16;

    private readonly Mutex _mutex;

    public SingleInstance(string dataFolder)
    {
        var name = NameFor(dataFolder);

        // Only whether the mutex already existed matters, so no thread needs to own it.
        // Local\ keeps the name within this sign-in session.
        _mutex = new Mutex(initiallyOwned: false, @"Local\" + name, out var isFirst);
        IsFirst = isFirst;
    }

    /// <summary>
    /// True for the first copy, which keeps running. False means another copy already runs.
    /// </summary>
    public bool IsFirst { get; }

    public void Dispose()
    {
        _mutex.Dispose();
    }

    /// <summary>
    /// The mutex and pipe name. Every spelling of a folder gets the same name.
    /// </summary>
    private static string NameFor(string dataFolder)
    {
        var folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataFolder)).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(folder)));
        return $"{AppInfo.Name}.{hash[..HashDigits]}";
    }
}
