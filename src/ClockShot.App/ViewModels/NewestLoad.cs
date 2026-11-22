namespace ClockShot.App.ViewModels;

/// <summary>
/// Lets only the newest of a series of loads finish.
/// Starting a load cancels the one before it.
/// </summary>
public sealed class NewestLoad : IDisposable
{
    private CancellationTokenSource? _current;
    private bool _isDisposed;

    /// <summary>
    /// Cancels the previous load and returns the token for a new one.
    /// After disposing, it returns a token that's already cancelled.
    /// A load started during shutdown then stops at once.
    /// </summary>
    public CancellationToken Start()
    {
        if (_isDisposed)
        {
            return new CancellationToken(canceled: true);
        }

        // The old source is only cancelled.
        // It holds no timer, so the garbage collector can free it.
        _current?.Cancel();
        _current = new CancellationTokenSource();
        return _current.Token;
    }

    /// <summary>
    /// Cancels the current load without starting another.
    /// </summary>
    public void Cancel()
    {
        if (!_isDisposed)
        {
            _current?.Cancel();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _current?.Cancel();
        _current?.Dispose();
    }
}
