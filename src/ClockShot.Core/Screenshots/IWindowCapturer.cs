namespace ClockShot.Core.Screenshots;

public interface IWindowCapturer
{
    /// <summary>
    /// Captures the screen where the window is, including anything on top of it.
    /// Returns null when the window can't be captured, such as when it's minimised.
    /// </summary>
    CapturedImage? Capture(nint windowHandle);
}
