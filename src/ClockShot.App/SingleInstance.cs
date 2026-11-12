using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

using ClockShot.Core;

namespace ClockShot.App;

/// <summary>
/// Keeps one running copy per data folder, because two would record everything twice.
/// A second launch asks the running copy to show its window through a named pipe, then exits.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string ShowMessage = "show";

    /// <summary>
    /// Enough hex digits to tell folders apart. Few enough to read in a debugger.
    /// </summary>
    private const int HashDigits = 16;

    /// <summary>
    /// How long a second copy waits for the running copy to answer.
    /// </summary>
    private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(3);

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly TimeSpan _connectTimeout;
    private readonly CancellationTokenSource _stopping = new();

    public SingleInstance(string dataFolder)
        : this(dataFolder, DefaultConnectTimeout)
    {
    }

    /// <param name="dataFolder">The folder this copy keeps its data in.</param>
    /// <param name="connectTimeout">How long a second copy waits for the running copy to answer.</param>
    public SingleInstance(string dataFolder, TimeSpan connectTimeout)
    {
        _connectTimeout = connectTimeout;
        var name = NameFor(dataFolder);

        // Only whether the mutex already existed matters, so no thread needs to own it.
        // Local\ keeps the name within this sign-in session.
        _mutex = new Mutex(initiallyOwned: false, @"Local\" + name, out var isFirst);
        IsFirst = isFirst;
        _pipeName = name;
    }

    /// <summary>
    /// True for the first copy, which keeps running. False means another copy already runs.
    /// </summary>
    public bool IsFirst { get; }

    /// <summary>
    /// From a second copy, asks the running copy to show its window.
    /// It gives up quietly after a few seconds.
    /// </summary>
    public void AskRunningCopyToShow()
    {
        try
        {
            using var pipe = new NamedPipeClientStream(
                serverName: ".",
                pipeName: _pipeName,
                direction: PipeDirection.Out,
                options: PipeOptions.CurrentUserOnly);
            pipe.Connect(_connectTimeout);
            using var writer = new StreamWriter(pipe);
            writer.WriteLine(ShowMessage);
        }
        catch (Exception ex) when (ex is TimeoutException or IOException)
        {
            // The running copy is busy or closing, so there's nothing more to do.
        }
    }

    /// <summary>
    /// In the first copy, calls <paramref name="show"/> whenever another copy is launched.
    /// The call comes on a background thread.
    /// </summary>
    public void ListenForLaunches(Action show)
    {
        // Read the token now, because _stopping.Token throws once Dispose has run.
        var stopping = _stopping.Token;
        _ = Task.Run(async () =>
        {
            while (!stopping.IsCancellationRequested)
            {
                try
                {
                    await WaitForLaunchAsync(show, stopping);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (IOException)
                {
                    // A copy disconnected early. Wait for the next one.
                }
            }
        });
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
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

    /// <summary>
    /// Waits for one copy to connect, and calls <paramref name="show"/> if it asks.
    /// </summary>
    private async Task WaitForLaunchAsync(Action show, CancellationToken stopping)
    {
        await using var pipe = new NamedPipeServerStream(
            pipeName: _pipeName,
            direction: PipeDirection.In,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.WaitForConnectionAsync(stopping);
        using var reader = new StreamReader(pipe);
        if (await reader.ReadLineAsync(stopping) == ShowMessage)
        {
            show();
        }
    }
}
