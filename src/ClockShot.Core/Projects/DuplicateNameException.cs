namespace ClockShot.Core.Projects;

/// <summary>
/// A sibling already has this name.
/// Names are unique among siblings, ignoring case.
/// </summary>
public sealed class DuplicateNameException(string name) : Exception($"\"{name}\" already exists here.");
