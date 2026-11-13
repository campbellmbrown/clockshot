using System.Diagnostics.CodeAnalysis;

using ClockShot.Core.Models;

namespace ClockShot.Core.Tracking;

/// <summary>
/// Something the <see cref="ActivityTracker"/> wants recorded.
/// There's at most one open activity segment and one open computer-state segment at a time.
/// Ended and extended always refer to the open one.
/// </summary>
[SuppressMessage("Sonar", "S2094", Justification = "The writer handles every change kind in one switch.")]
public abstract record TrackingChange;

/// <summary>
/// A new activity segment starts, because the user switched window or came back.
/// </summary>
public sealed record ActivityStarted(DateTime StartUtc, ForegroundWindow Window) : TrackingChange;

/// <summary>
/// The open activity segment ends.
/// </summary>
public sealed record ActivityEnded(DateTime EndUtc) : TrackingChange;

/// <summary>
/// A new computer-state segment starts.
/// </summary>
public sealed record StateStarted(DateTime StartUtc, ComputerState State) : TrackingChange;

/// <summary>
/// The open computer-state segment ends.
/// </summary>
public sealed record StateEnded(DateTime EndUtc) : TrackingChange;

/// <summary>
/// The open segments are still going. Their end is now at least <paramref name="EndUtc"/>.
/// </summary>
public sealed record SegmentsExtended(DateTime EndUtc) : TrackingChange;
