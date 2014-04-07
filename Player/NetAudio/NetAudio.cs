using System;
using System.Runtime.Serialization;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DriftPlayer
{
    class NetAudio : IPlayable
    {
        private readonly string url;
        private MediaFoundationReader reader;
        private WaveOutEvent waveOut;
        private VolumeSampleProvider volumeProvider;
        private float volume;
        private bool isInit;
        private PlaybackState state;
        private readonly string title;
        private bool isAdded;


        public NetAudio(string url)
        {
            this.isAdded = false;
            this.state = DriftPlayer.PlaybackState.Stopped;
            this.title = this.url = url;
        }

        public void Play()
        {
            if (this.state == DriftPlayer.PlaybackState.Playing) return;
            if (this.state == DriftPlayer.PlaybackState.Stopped)
            {
                this.waveOut = new WaveOutEvent();
                waveOut.PlaybackStopped += waveOut_PlaybackStopped;
                this.reader = new MediaFoundationReader(this.url);
                this.volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider());
                this.waveOut.Init(volumeProvider);
                this.volumeProvider.Volume = this.volume;
                this.isInit = true;
                this.waveOut.Play();
            }
            else if (this.state == DriftPlayer.PlaybackState.Paused)
            {
                this.volumeProvider.Volume = this.volume;
                waveOut.Play();
            }

            this.state = DriftPlayer.PlaybackState.Playing;
        }

        private void waveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (PlaybackFinished != null)
                PlaybackFinished(this, EventArgs.Empty);
        }

        public void Pause()
        {
            if (this.state == DriftPlayer.PlaybackState.Paused) return;
            if (this.isInit)
            {
                waveOut.Pause();
                this.state = DriftPlayer.PlaybackState.Paused;
            }
        }

        public void Stop()
        {
            if (this.state == DriftPlayer.PlaybackState.Stopped) return;
            if (this.isInit)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut.Dispose();
                reader.Dispose();
                this.state = DriftPlayer.PlaybackState.Stopped;
            }
        }

        public float Volume
        {
            get
            {
                if (this.volumeProvider != null)
                    return this.volumeProvider.Volume;
                else
                    return this.volume;
            }
            set
            {
                if (this.volumeProvider != null)
                    this.volumeProvider.Volume = value;
                else
                    this.volume = value;
            }
        }

        public event EventHandler PlaybackFinished;

        public PlaybackState PlaybackState
        {
            get { return this.state; }
        }

        public string Title
        {
            get { return this.title; }
        }

        public event EventHandler PlayableReady;

        public void Init()
        {
            if (PlayableReady != null && !this.isAdded)
            {
                PlayableReady(this, EventArgs.Empty);
                this.isAdded = true;
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                if (this.reader != null)
                    return this.reader.TotalTime;

                return TimeSpan.Zero;
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                if (this.reader != null)
                    return this.reader.CurrentTime;

                return TimeSpan.Zero;
            }
            set
            {
                if (this.reader != null)
                    this.reader.CurrentTime = value;
            }
        }

        public NetAudio(SerializationInfo info, StreamingContext ctxt)
            :this((string)info.GetValue("Url", typeof(string)))
        { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Url", this.url);
        }
    }
}
