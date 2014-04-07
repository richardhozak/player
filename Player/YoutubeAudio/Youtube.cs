using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DriftPlayer
{
    static class Youtube
    {
        private static readonly Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "youtube-dl.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        public static /*async*/ Task<VideoInfo> GetYoutubeDownloadUrlAsync(string youtubeUri)
        {
            return Task.Run(() =>
            {
                return GetYoutubeDownloadUrl(youtubeUri);
            });
            //Youtube.process.StartInfo.Arguments = String.Format("-g -e -f mp4 {0}", youtubeUri);
            //Youtube.process.Start();
            //VideoInfo info = new VideoInfo();
            //info.Title = await process.StandardOutput.ReadLineAsync();
            //info.Url = await process.StandardOutput.ReadLineAsync();
            //return info;
        }

        public static VideoInfo GetYoutubeDownloadUrl(string youtubeUri)
        {
            Console.WriteLine("Fetching youtube info...");
            Console.WriteLine("Url/Id: " + youtubeUri);

            string uri = Youtube.IsId(youtubeUri) ? String.Format("http://www.youtube.com/watch?v={0}", youtubeUri) : youtubeUri;

            Youtube.process.StartInfo.Arguments = String.Format("-g -e -f 140 {0}", uri);
            Youtube.process.Start();
            VideoInfo info = new VideoInfo();

            info.Title = process.StandardOutput.ReadLine();
            Console.WriteLine("Title: " + info.Title);

            info.Url = process.StandardOutput.ReadLine();
            Console.WriteLine("Video url: " + info.Url + "\r\n");

            return info;
        }

        private static bool IsId(string youtubeUri)
        {
            return youtubeUri.Length == 11;
        }
    }
}
