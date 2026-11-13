namespace ClockShot.Core.Models;

/// <summary>
/// What the computer was doing. Time with no segment at all means ClockShot was not running.
/// </summary>
/// <remarks>
/// The numbers are stored in the database, so never change them.
/// </remarks>
public enum ComputerState
{
    Active = 0,
    Away = 1,
}
