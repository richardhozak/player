using System;
using System.Runtime.Serialization;

namespace DriftPlayer
{
    public interface IPlayable : ISerializable
    {
        void Play();
        void Pause();
        void Stop();
        float Volume { get; set; }
        PlaybackState PlaybackState { get; }
        event EventHandler PlaybackFinished;
        string Title { get; }
        event EventHandler PlayableReady;
        void Init();
        TimeSpan TotalTime { get; }
        TimeSpan CurrentTime { get; set; }
    }
}
