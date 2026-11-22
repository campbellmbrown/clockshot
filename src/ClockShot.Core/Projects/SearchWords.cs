namespace ClockShot.Core.Projects;

/// <summary>
/// The words typed in a search box.
/// A name matches when it contains every word, in any order, ignoring case.
/// </summary>
public sealed class SearchWords
{
    private readonly string[] _words;

    private SearchWords(string[] words) => _words = words;

    /// <summary>
    /// No words, which matches every name.
    /// </summary>
    public static SearchWords None { get; } = new([]);

    public bool IsEmpty => _words.Length == 0;

    public static SearchWords Of(string? text) => new((text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public bool AreAllIn(string name) => _words.All(word => name.Contains(word, StringComparison.CurrentCultureIgnoreCase));
}
