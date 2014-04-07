using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DriftPlayer
{
    public class OutputProvider : IWavePlayer
    {
        private static Action<Output> outputChanged;
        private static Output output = Output.ASIO;

        public static Output Output
        {
            get
            {
                return OutputProvider.output;
            }
            set
            {
                OutputProvider.output = value;
                if (OutputProvider.outputChanged != null)
                    OutputProvider.outputChanged(output);
            }
        }

        //public static void ChangeOutput(Output output)
        //{
        //    OutputProvider.output = output;
        //    if (OutputProvider.outputChanged != null)
        //        OutputProvider.outputChanged(output);
        //}

        private IWaveProvider waveProvider;
        private IWavePlayer wavePlayer;

        public OutputProvider()
        {
            OutputProvider.outputChanged += OutputChanged;
        }

        private void OutputChanged(Output obj)
        {
            if (this.wavePlayer != null)
            {
                switch (this.wavePlayer.PlaybackState)
                {
                    case NAudio.Wave.PlaybackState.Stopped:
                        this.wavePlayer.Dispose();
                        this.Init(this.waveProvider);
                        break;
                    case NAudio.Wave.PlaybackState.Playing:
                        this.wavePlayer.Stop();
                        this.wavePlayer.Dispose();
                        this.Init(this.waveProvider);
                        this.wavePlayer.Play();
                        break;
                    case NAudio.Wave.PlaybackState.Paused:
                        this.wavePlayer.Stop();
                        this.wavePlayer.Dispose();
                        this.Init(this.waveProvider);
                        this.wavePlayer.Pause();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void Init(IWaveProvider waveProvider)
        {
            this.waveProvider = waveProvider;

            switch (OutputProvider.output)
            {
                case Output.ASIO:
                    AsioOut asio = new AsioOut();
                    asio.ChannelOffset = 0;
                    ISampleProvider s = new SampleChannel(this.waveProvider);
                    this.wavePlayer = asio;
                    this.wavePlayer.Init(s);
                    break;
                case Output.WAVE:
                    this.wavePlayer = new WaveOutEvent();
                    this.wavePlayer.Init(this.waveProvider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            
            ////sampleChannel.PreVolumeMeter += OnPreVolumeMeter;
            ////this.setVolumeDelegate = (vol) => sampleChannel.Volume = vol;
            //var postVolumeMeter = new MeteringSampleProvider(sampleChannel);
            ////postVolumeMeter.StreamVolume += OnPostVolumeMeter;

            
            this.wavePlayer.PlaybackStopped += this.PlaybackStopped;
        }

        public void Pause()
        {
            this.wavePlayer.Pause();
        }

        public void Play()
        {
            this.wavePlayer.Play();
        }

        public NAudio.Wave.PlaybackState PlaybackState
        {
            get
            {
                return this.wavePlayer.PlaybackState;
            }
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public void Stop()
        {
            this.wavePlayer.Stop();
        }

        public float Volume
        {
            get
            {
                return this.wavePlayer.Volume;
            }
            set
            {
                this.wavePlayer.Volume = value;
            }
        }

        public void Dispose()
        {
            this.wavePlayer.Dispose();
        }
    }
}
