namespace ClockShot.Core.Models;

/// <summary>
/// The colour chosen for a tag, which is a project and task pair.
/// A null <paramref name="TaskId"/> means the project's General bucket.
/// Tags without one get an automatic colour from their name (see <see cref="Projects.ProjectCatalog.ColorOf"/>).
/// </summary>
public sealed record TagColor(long ProjectId, long? TaskId, string ColorHex);
