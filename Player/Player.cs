using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ZNet;

namespace DriftPlayer
{
    public class Player
    {
        private IPlayable currentlyPlaying;
        private float volume;
        //private Playlist<IPlayable> playlist;
        private List<IPlayable> playlist;
        //private HttpListener listener;

        public EventHandler PlayableAdded;
        public EventHandler PlayableRemoved;

        private readonly ZTcpServer tcpServer;
        private readonly ZUdpServer udpServer;

        public List<IPlayable> Playlist
        {
            get
            {
                return this.playlist;
            }
        } 

        public Player()
        {
            //TcpServer.Start();
            this.playlist = initializePlaylist();
            //this.playlist = new List<IPlayable>();
            this.currentlyPlaying = null;
            
            //initializePlaylist();

            //this.tcpServer = new ZTcpServer(10000);
            //this.udpServer = new ZUdpServer(10000);
            //this.tcpServer.ClientConnected += tcpServer_ClientConnected;
            //this.tcpServer.ClientDisconnected += tcpServer_ClientDisconnected;
            //this.tcpServer.MessageReceived += server_MessageReceived;
            //this.udpServer.MessageReceived += server_MessageReceived;
            //this.tcpServer.Start();
            //this.udpServer.Start();



            //this.listener = new HttpListener();
            //listener.Prefixes.Add("http://localhost:58403/");
            //listener.Prefixes.Add("10.0.0.2:58402");
            //listener.Start();
            //listener.BeginGetContext(new AsyncCallback(requestCallback), null);
        }

        void server_MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message == null)
                return;

            switch (e.Message)
            {
                case "#play":
                    this.Play();
                    break;
                case "#pause":
                    this.Pause();
                    break;
                case "#stop":
                    this.Stop();
                    break;
                default:
                    {
                        double d;
                        if (Double.TryParse(e.Message, out d))
                        {
                            this.Volume = (float)d;
                        }
                        break;
                    }
            }
        }

        void tcpServer_ClientDisconnected(object sender, ClientEventArgs e)
        {
            this.udpServer.RemoveClient(e.ClientEndPoint);
        }

        void tcpServer_ClientConnected(object sender, ClientEventArgs e)
        {
            this.udpServer.AddClient(e.ClientEndPoint);
        }

        private List<IPlayable> initializePlaylist()
        {
            FileInfo fi = new FileInfo("playlist.dpl");
            if (fi.Exists)
            {
                try
                {
                    using (Stream stream = File.Open(fi.Name, FileMode.Open))
                    {
                        BinaryFormatter bin = new BinaryFormatter();

                        return (List<IPlayable>)bin.Deserialize(stream);
                        //foreach (IPlayable track in tracks)
                        //{
                        //    //this.Add(track);
                        //    this.Add(track);
                        //}
                    }
                }
                catch (IOException)
                {
                }
            }
            return new List<IPlayable>();
        }

        //private void requestCallback(IAsyncResult ar)
        //{
        //    HttpListenerContext context = listener.EndGetContext(ar);
        //    HttpListenerRequest request = context.Request;
        //    HttpListenerResponse response = context.Response;

        //    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        //    {
        //        var postData = reader.ReadToEnd().Split('=');
        //        if (postData[0] == "url")
        //        {
        //            try
        //            {
        //                var asd = new YoutubeAudio(Uri.UnescapeDataString(postData[1]));
        //                asd.PlaybackFinished += (s, e) =>
        //                {
        //                    //Next();
        //                };
        //                this.Add(asd);
        //            }
        //            catch { }
        //        }
        //    }
        //    response.ContentType = "text/plain";
        //    response.StatusCode = (int)HttpStatusCode.OK;
        //    response.StatusDescription = HttpStatusCode.OK.ToString();
        //    byte[] buffer = Encoding.UTF8.GetBytes("ACK");
        //    response.ContentLength64 = buffer.Length;
        //    response.OutputStream.Write(buffer, 0, buffer.Length);
        //    response.OutputStream.Close();
        //    response.Close();
        //    listener.BeginGetContext(new AsyncCallback(requestCallback), null);
        //}

        public void Add(IPlayable track)
        {
            //track.PlaybackFinished += (s, e) => 
            //{
            //    Next();
            //};
            if (this.currentlyPlaying == null)
                this.currentlyPlaying = track;
            this.playlist.Add(track);
            track.PlayableReady += (s, e) => 
            {
                if (PlayableAdded != null)
                    PlayableAdded(s, EventArgs.Empty);
            };
            track.Init();
        }

        public void Play()
        {
            if (this.currentlyPlaying == null)
                return;

            currentlyPlaying.Play();
            currentlyPlaying.Volume = this.volume;
            //if (currentlyPlaying != null)
            //{
            //    if (currentlyPlaying.PlaybackState != PlaybackState.Playing)
            //        currentlyPlaying.Volume = volume;
            //    currentlyPlaying.Play();
            //}
            //else if (currentlyPlayingIndex == -1)
            //{
            //    if (playlist.Count > 0)
            //    {
            //        this.currentlyPlaying = playlist[0];
            //        this.currentlyPlayingIndex = 0;
            //        currentlyPlaying.Volume = volume;
            //        currentlyPlaying.Play();
            //    }
            //}
        }

        public void Play(IPlayable playable)
        {
            int index = playlist.IndexOf(playable);
            if (index != -1)
            {
                //this.playlist.Select(playable);
                if (currentlyPlaying != null)
                    currentlyPlaying.Stop();
                currentlyPlaying = playable;
                //currentlyPlaying.PlaybackFinished += (s, e) => 
                //{
                //    Next();
                //};
                Play();
            }
        }

        public void Remove(IPlayable playable)
        {
            if (playable == this.currentlyPlaying)
            {
                if (currentlyPlaying != null)
                    currentlyPlaying.Stop();
                //this.currentlyPlaying = playlist.Next();
                this.playlist.Remove(playable);
            }
            else
                this.playlist.Remove(playable);

            if (PlayableRemoved != null)
                PlayableRemoved(playable, EventArgs.Empty);
        }

        public void Stop()
        {
            if (currentlyPlaying != null)
                currentlyPlaying.Stop();
        }

        public void Pause()
        {
            if (currentlyPlaying != null)
                currentlyPlaying.Pause();
        }

        public void Next()
        {
            //if (currentlyPlayingIndex < playlist.Count - 1 && currentlyPlaying != null)
            //{
            //    currentlyPlayingIndex++;
            //    currentlyPlaying.Stop();
            //    currentlyPlaying = playlist[currentlyPlayingIndex];
            //    Play();
            //}
            if (currentlyPlaying != null)
                currentlyPlaying.Stop();
            if (this.playlist.Count > 1)
            {
                int index = this.playlist.IndexOf(currentlyPlaying) + 1;
                this.currentlyPlaying = playlist[index == this.playlist.Count ? 0 : index];
            }
            this.Play();
        }

        public void Previous()
        {
            if (this.currentlyPlaying != null)
                this.currentlyPlaying.Stop();
                

            if (this.playlist.Count > 1)
            {
                int index = this.playlist.IndexOf(this.currentlyPlaying) - 1;
                this.currentlyPlaying = playlist[index == -1 ? this.playlist.Count - 1 : index];
            }
            this.Play();
        }

        //public void Prev()
        //{
        //    //if (currentlyPlayingIndex > 0 && currentlyPlaying != null)
        //    //{
        //    //    currentlyPlayingIndex--;
        //    //    currentlyPlaying.Stop();
        //    //    currentlyPlaying = playlist[currentlyPlayingIndex];
        //    //    Play();
        //    //}
        //    if (currentlyPlaying != null)
        //        currentlyPlaying.Stop();
        //    Play();
        //}

        public float Volume
        {
            get 
            {
                if (currentlyPlaying != null)
                    return currentlyPlaying.Volume;
                else
                    return this.volume;
            }
            set 
            {
                this.volume = value;
                if (currentlyPlaying != null)
                    currentlyPlaying.Volume = value;
            }
        }

        public void Dispose()
        {
            foreach (IPlayable p in this.playlist)
            {
                p.Stop();
            }
            using (Stream stream = File.Open("playlist.dpl", FileMode.Create))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                bformatter.Serialize(stream, this.playlist);    
            }
        }

        

        public TimeSpan CurrentTime
        {
            get 
            {
                if (this.currentlyPlaying != null)
                    return this.currentlyPlaying.CurrentTime;

                return TimeSpan.Zero;
            }
            set
            {
                if (this.currentlyPlaying != null)
                    this.currentlyPlaying.CurrentTime = value;
            }
        }

        public TimeSpan TotalTime
        {
            get 
            {
                if (this.currentlyPlaying != null)
                    return this.currentlyPlaying.TotalTime;

                return TimeSpan.Zero;
            }
        }

        public PlaybackState State
        {
            get
            {
                return this.currentlyPlaying != null ? this.currentlyPlaying.PlaybackState : PlaybackState.Stopped;
            }
        }
        
    }
}
