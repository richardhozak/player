namespace DriftPlayer
{
    public class VolumeEventArgs
    {
        public VolumeEventArgs(double volume)
        {
            this.Volume = volume;
        }

        public readonly double Volume;
    }
}
