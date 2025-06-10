using System.Diagnostics;

namespace SETargetCamera
{
    public class DisplayFrameTimer
    {
        public static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        public static double TimeSinceUpdateMs;
    }
}