namespace ClockShot.Core;

/// <summary>
/// Keeps up to a fixed number of items, and drops the least recently used one to make room for a new one.
/// Each lookup scans every item, so this suits a small capacity.
/// It's safe to use from more than one thread.
/// </summary>
public sealed class RecentlyUsedCache<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    private readonly int _capacity;
    private readonly Lock _lock = new();

    // The most recently used item comes first.
    private readonly List<KeyValuePair<TKey, TValue>> _items = [];

    public RecentlyUsedCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    /// <summary>
    /// The value for <paramref name="key"/>, or null when it isn't kept.
    /// Finding it makes it the most recently used.
    /// </summary>
    public TValue? Find(TKey key)
    {
        lock (_lock)
        {
            var index = IndexOf(key);
            if (index < 0)
            {
                return null;
            }

            var item = _items[index];
            _items.RemoveAt(index);
            _items.Insert(0, item);
            return item.Value;
        }
    }

    /// <summary>
    /// Keeps <paramref name="value"/> as the most recently used.
    /// It replaces any value already kept for <paramref name="key"/>.
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            var index = IndexOf(key);
            if (index >= 0)
            {
                _items.RemoveAt(index);
            }

            _items.Insert(0, new KeyValuePair<TKey, TValue>(key, value));
            if (_items.Count > _capacity)
            {
                _items.RemoveAt(_items.Count - 1);
            }
        }
    }

    private int IndexOf(TKey key) => _items.FindIndex(item => EqualityComparer<TKey>.Default.Equals(item.Key, key));
}
