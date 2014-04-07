using System;
using System.Net;
using System.Xml.Linq;

namespace DriftPlayer
{
    static class Soundcloud
    {
        private const string clientId = "ab19cdfa67211da290779ce0f15f67a3";

        public static VideoInfo GetDownloadUrl(string soundcloudUrl)
        {
            Console.WriteLine("Fetching soundcloud info...");
            Console.WriteLine("Url: " + soundcloudUrl);
            
            VideoInfo info = new VideoInfo();

            WebClient client = new WebClient();

            string resolve = String.Format("http://api.soundcloud.com/resolve.xml?url={0}", soundcloudUrl);

            Console.WriteLine("Resolve url: " + resolve);

            string response = client.DownloadString(resolve + "&client_id=" + Soundcloud.clientId);

            XDocument xml = XDocument.Parse(response);

            XElement track = xml.Element("track");
            string url = track.Element("stream-url").Value;
            //string user = track.Element("user").Element("username").Value;
            string title = track.Element("title").Value;

            //string title = String.Format("{0} - {1}", user, trackname);

            Console.WriteLine("Stream url: " + url);
            Console.WriteLine("Title: " + title);

            info.Title = title;
            info.Url = url + "?client_id=" + Soundcloud.clientId;

            return info;
        }
    }
}
