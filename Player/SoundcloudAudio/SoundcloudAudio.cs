using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DriftPlayer
{
    [Serializable]
    class SoundcloudAudio : IPlayable
    {
        private readonly string url;
        private bool isAdded;
        private PlaybackState state;
        private WaveOutEvent waveOut;
        private MediaFoundationReader reader;
        private VolumeSampleProvider volumeProvider;
        private bool isInit;
        private float volume;
        private string title;
        private string downloadUrl;

        public SoundcloudAudio(string url)
        {
            // /^https?:\/\/(soundcloud.com|snd.sc)\/(.*)$/

            Match match = Regex.Match(url, "https?://(soundcloud.com|snd.sc)/(.*)/(.*)");
            if (match.Success)
            {
                this.url = url;
            }
            else
            {
                throw new ArgumentException("Neplatné video.");
            }
        }

        public void Play()
        {
            if (this.state == DriftPlayer.PlaybackState.Playing) return;
            if (this.state == DriftPlayer.PlaybackState.Stopped)
            {
                this.waveOut = new WaveOutEvent();
                waveOut.PlaybackStopped += waveOut_PlaybackStopped;
                this.reader = new MediaFoundationReader(this.downloadUrl);
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
            VideoInfo info = Soundcloud.GetDownloadUrl(this.url);
            this.downloadUrl = info.Url;
            this.title = info.Title;

            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.downloadUrl);
            //request.AllowAutoRedirect = false;
            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //this.playbackUrl = response.Headers["Location"];


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

        public SoundcloudAudio(SerializationInfo info, StreamingContext ctxt)
        {
            this.isAdded = true;
            this.title = (string)info.GetValue("Title", typeof(string));
            this.url = (string)info.GetValue("Url", typeof(string));
            this.downloadUrl = (string)info.GetValue("DownloadUrl", typeof(string));
        }

        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("Title", this.title);
            info.AddValue("Url", this.url);
            info.AddValue("DownloadUrl", this.downloadUrl);
        }
    }
}
