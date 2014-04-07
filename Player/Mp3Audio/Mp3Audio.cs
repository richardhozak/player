using NAudio.Wave;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace DriftPlayer
{
    [Serializable()]
    class Mp3Audio : IPlayable, ISerializable
    {
        private string title;
        private IWavePlayer waveOut;
        private AudioFileReader mainOutputStream;//was WaveStream
        //private WaveChannel32 volumeStream;
        private FileInfo file;
        private PlaybackState state;
        private float volume;
        private bool isInit;
        private bool isAdded;

        public Mp3Audio(FileInfo fi)
            :this(fi.FullName)
        { }

        public Mp3Audio(string path)
        {
            this.isAdded = false;
            this.file = new FileInfo(path);
            if (!this.file.Exists)
                throw new Exception();

            if (!this.IsSupported(this.file.Extension))
                throw new Exception();
            
            //if (!path.EndsWith(".mp3"))
            //    throw new Exception();

            this.title = this.file.Name;
            this.Volume = volume;
            this.state = DriftPlayer.PlaybackState.Stopped;
            this.isInit = false;
            
        }


        //*.wav;*.aiff;*.mp3;*.aac;*.flac;*.ogg;*.wma
        private bool IsSupported(string extension)
        {
            switch (extension)
            {
                case ".mp3":
                case ".wma":
                case ".wav":
                case ".flac":
                case ".ogg":
                case ".aiff":
                case ".aac":
                    return true;
                default:
                    return false;
            }
        }

        private WaveStream GetStreamFromFileName(string filename)
        {
            string ext = Path.GetExtension(filename);
            switch (ext)
            {
                case ".mp3":
                case ".wma":
                case ".wav":
                case ".flac":
                case ".ogg":
                    return new AudioFileReader(filename);
                //case ".flac":
                //    return new FLACFileReader(filename);
            }
            return null;
        }

        private IWaveProvider CreateInputStream(string fileName)
        {
            //IWaveProvider inputStream;
            //WaveStream reader = new Mp3FileReader(fileName);
            //WaveStream readerStream = this.GetStreamFromFileName(fileName);
            //new VolumeWaveProvider16()


            //this.mainOutputStream = this.GetStreamFromFileName(fileName);
            

            //IWaveProvider asd = new MediaFoundationResampler(this.mainOutputStream, new WaveFormat());

            //new MediaFoundationEncoder(new MediaType())

            //if b (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            //{
            //    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
            //    readerStream = new BlockAlignReductionStream(readerStream);
            //}

            //new VolumeSampleProvider(readerStream.ToSampleProvider());
            //this.volumeStream = new VolumeSampleProvider(readerStream.ToSampleProvider());
            //this.volumeStream = null;//new VolumeWaveProvider16(readerStream);// new WaveChannel32(readerStream);

            //if (Path.GetExtension(fileName) == ".mp3")
            //{
            //    this.mainOutputStream = new BlockAlignReductionStream(this.mainOutputStream);
            //    // Wave channel - reads from file and returns raw wave blocks
            //    this.volumeStream = new WaveChannel32(this.mainOutputStream);
            //    return this.volumeStream;
            //}

            //if (this.mainOutputStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            //{
            //    this.mainOutputStream = WaveFormatConversionStream.CreatePcmStream(this.mainOutputStream);
            //    this.mainOutputStream = new BlockAlignReductionStream(this.mainOutputStream);
            //}
            //if (this.mainOutputStream.WaveFormat.BitsPerSample != 16)
            //{
            //    var format = new WaveFormat(this.mainOutputStream.WaveFormat.SampleRate, 16, this.mainOutputStream.WaveFormat.Channels);
            //    this.mainOutputStream = new WaveFormatConversionStream(format, this.mainOutputStream);
            //}

            //this.volumeStream = new WaveChannel32(this.mainOutputStream);
            
            
            
            //inputStream = new WaveChannel32(readerStream);
            //volumeStream = inputStream;

            return null;
        }

        void waveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (PlaybackFinished != null)
                PlaybackFinished(this, EventArgs.Empty);
        }

        public void Play()
        {
            if (!this.isInit)
                Init();
            this.state = DriftPlayer.PlaybackState.Playing;
            waveOut.Play();
        }

        public void Pause()
        {
            if (!this.isInit)
                return;
            this.state = DriftPlayer.PlaybackState.Paused;
            waveOut.Pause();
        }

        public void Stop()
        {
            if (this.state == DriftPlayer.PlaybackState.Stopped) return;
            if (this.isInit)
            {
                this.state = DriftPlayer.PlaybackState.Stopped;
                waveOut.Stop();
                waveOut.Dispose();
                mainOutputStream.Dispose();
                //volumeStream.Dispose();
                waveOut.Dispose();
                isInit = false;
            }
        }

        public float Volume
        {
            get
            {
                //if (this.volumeStream != null)
                //    return this.volumeStream.Volume;
                //else
                //    return this.volume;

                if (this.mainOutputStream != null)
                    return this.mainOutputStream.Volume;

                return this.mainOutputStream.Volume;
            }
            set
            {
                //if (this.volumeStream != null)
                //    this.volumeStream.Volume = value;
                //else
                //    this.volume = value;

                if (this.mainOutputStream != null)
                    this.mainOutputStream.Volume = value;

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
            this.waveOut = new WaveOutEvent();
            waveOut.PlaybackStopped += waveOut_PlaybackStopped;
            this.mainOutputStream = new AudioFileReader(this.file.FullName);
            //this.mainOutputStream = CreateInputStream(file.FullName);
            //waveOut.Init(CreateInputStream(file.FullName));
            waveOut.Init(this.mainOutputStream);
            if (PlayableReady != null && !this.isAdded)
            {
                PlayableReady(this, EventArgs.Empty);
                this.isAdded = true;
            }
            this.isInit = true;
        }

        public Mp3Audio(SerializationInfo info, StreamingContext ctxt)
            :this((string)info.GetValue("Path", typeof(string)))
        { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Path", file.FullName);
        }


        public TimeSpan TotalTime
        {
            get 
            {
                if (mainOutputStream != null)
                    return mainOutputStream.TotalTime;

                return TimeSpan.Zero;
            }
        }

        public TimeSpan CurrentTime
        {
            get 
            {
                if (mainOutputStream != null)
                    return mainOutputStream.CurrentTime;

                return TimeSpan.Zero;
            }
            set
            {
                if (mainOutputStream != null)
                    mainOutputStream.CurrentTime = value;
            }
        }
    }
}
