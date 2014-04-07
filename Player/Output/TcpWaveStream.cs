using NAudio.Wave;

namespace DriftPlayer
{
    class TcpWaveStream : WaveStream
    {
        private readonly WaveStream waveStream;

        public TcpWaveStream(WaveStream waveStream)
        {
            this.waveStream = waveStream;
        }

        public override WaveFormat WaveFormat
        {
            get
            {
                return this.waveStream.WaveFormat;
            }
        }

        public override long Length
        {
            get
            {
                return this.waveStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.waveStream.Position;
            }
            set
            {
                this.waveStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = this.waveStream.Read(buffer, offset, count);
            TcpServer.Write(buffer, offset, count);
            return read;
        }
    }
}
