using System;

namespace DriftPlayer
{
    public class TimeEventArgs : EventArgs
    {
        public TimeSpan Time { get; private set; }
        public TimeEventArgs(TimeSpan time)
        {
            this.Time = time;
        }
    }
}
