using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Threading;
using Newtonsoft.Json;
using Jurassic;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace DriftPlayer
{
    [Serializable]
    public class YoutubeAudio : IPlayable
    {
        private YoutubeUri youtubeUri;
        private PlaybackState state;
        private readonly string id;
        private MediaFoundationReader reader;
        private IWavePlayer waveOut;
        private VolumeSampleProvider volumeProvider;
        private float volume;
        private bool isInit;
        private string title;
        private bool coughtException;
        private bool isAdded;

        public YoutubeAudio(string video)
        {
            this.coughtException = false;
            this.isAdded = false;
            this.Volume = volume;
            this.state = DriftPlayer.PlaybackState.Stopped;
            this.isInit = false;
            string text = video;
            if (text.Length != 11)
            {
                Match match = Regex.Match(text, "/?.*(?:youtu.be\\/|v\\/|u/\\w/|embed\\/|watch\\?.*&?v=)");
                if (match.Success)
                {
                    id = text.Substring(match.Length, 11);
                }
                else
                {
                    throw new ArgumentException("Neplatné video.");
                }
            }
            else
            {
                id = text;
            }
        }

        void waveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (PlaybackFinished != null)
                PlaybackFinished(this, EventArgs.Empty);
        }

        public void InitOld()
        {
            string uri = "http://www.youtube.com/watch?v=" + this.id + "&nomobile = 1";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
            // Cookie nefunguje muze se odstranit
            request.Headers["Cookie"] = "__utma=27069237.963674722.1351282862.1368037452.1369585269.67; __utmz=27069237.1351282862.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none); use_hitbox=d5c5516c3379125f43aa0d495d100d6ddAEAAAAw; ocSDy.resume=; demographics=c8daa0ea3a5b461633e082c4a1fa0fd4e3QDAAAAYWdlaRIAAAB0BgAAAGdlbmRlcnQBAAAAbTA=; ZxAcJ.resume=r_y-G9FIKLs:1108,Lusndl2BUpU:1944,gCScuk3NV4I:1137,4fizO5yBaas:634; VISITOR_INFO1_LIVE=N-hyto89vOI; LOGIN_INFO=; YSC=74lyq-RWIQY; recently_watched_video_id_list=af29336c4ca85469404a3a0168e40207WwcAAABzCwAAAGNHN2NSRGNQWTNrcwsAAABGS0lWejdrSUxuNHMLAAAAYThVUVNsdUM2MFVzCwAAAGViSE5JamhfYTkwcwsAAABSVEF0WUJjVklWMHMLAAAAcGtNaC1nTnBrMTRzCwAAAG1vY1FERVBhTWpn; HSID=A5fOFn8veqP0Jwkse; APISID=D-QcUoaP2B_43dPE/A3HUk2sWJfA7UKlaW; LOGIN_INFO=e4bdcb4a027f236aa20bb40f0452a2b5c4wAAAB7IjgiOiA0MDMxOTU5NDI5NjIsICIyIjogImdXSmI1VThQR1duTDJ4aTZjYUhSbmc9PSIsICIzIjogMzI3MDI4MDQ3LCAiMSI6IDEsICI2IjogdHJ1ZSwgIjciOiAxMzcxOTM0NjAwLCAiNCI6ICJHQUlBIiwgIjUiOiA1MDY2NTgzOTMyMjExNDUzfQ==; s_gl=1d69aac621b2f9c0a25dade722d6e24bcwIAAABVUw==; SID=DQAAANkAAADb6DZEcNjwzxz9LhsAx4e6xhupLPNZ9v6ZStO_-fZY1xYV-t_Zih-TqksRzhJuSmT8AOYKaxiHtMmGy2Fxfx6TFyTo_hg3rTABPKRhgsLSOsZXSSoz8bJ3HRHja1xhuouRLta-7bbnoZr-ndt4kYGrt-BfweatsqcyErbflg7X2lgB4ttwXEWYpyxtcE5TxTeuHafuiU8qoTuqMbDL6Uke8QSUL0VIzndD6T6PxB-OkSO1NyhYu30jVUNQtn52LyOganHkjhKw_iVe2xwekS09kxLkN8JNxJVnti_IrkDKAw; ACTIVITY=1371936699872; PREF=al=cs&f1=50000000&fv=11.7.700&f4=e10000";
            request.BeginGetResponse((r) => 
            {
                Stream stream = request.EndGetResponse(r).GetResponseStream();
                string response = new StreamReader(stream).ReadToEnd();
                this.title = Uri.UnescapeDataString(Regex.Match(response, "\"title\": \"(.*?)\"").Groups[1].Value).Replace("\\u0026", "&");
                var match = Regex.Match(response, "url_encoded_fmt_stream_map\": \"(.*?)\"");
                var data = Uri.UnescapeDataString(match.Groups[1].Value);
                //initOldData = data;
                var array = Regex.Split(data, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                var urls = new List<YoutubeUri>();
                try
                {
                    foreach (var d in array)
                    {
                        string url = "";
                        string signature = "";
                        string fallbackhost = "";
                        var ytUri = new YoutubeUri();
                        foreach (var p in d.Replace("\\u0026", "\t").Split('\t'))
                        {
                            var index = p.IndexOf('=');
                            if (index != -1 && index < p.Length)
                            {
                                try
                                {
                                    var key = p.Substring(0, index);
                                    var value = Uri.UnescapeDataString(p.Substring(index + 1));
                                    if (key == "url")
                                        url = value;
                                    else if (key == "itag")
                                        ytUri.Itag = int.Parse(value);
                                    else if (key == "type" && value.Contains("video/mp4"))
                                        ytUri.Type = value;
                                    else if (key == "sig")
                                        signature = value;
                                    else if (key == "s")
                                        signature = makeSignature(value);
                                    else if (key == "fallback_host")
                                        fallbackhost = value;
                                }
                                catch { }
                            }
                        }
                        ytUri.SetUrl(url + "&signature=" + signature);
                        if (ytUri.IsValid)
                            urls.Add(ytUri);
                    }
                    this.youtubeUri = urls[urls.Count - 1];
                    try
                    {
                        this.reader = new MediaFoundationReader(this.youtubeUri.Uri.AbsoluteUri);
                    }
                    catch (Exception ex)
                    {
                        this.coughtException = true;
                        if (ex.Message.Contains("E_ACCESSDENIED"))
                        {
                            string url = string.Format("http://www.youtube-mp3.org/a/pushItem/?item=http%3A//www.youtube.com/watch%3Fv%3D{0}&el=na&bf=false", id);
                            HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create(url);
                            request2.AutomaticDecompression = DecompressionMethods.GZip;
                            request2.Method = "GET";
                            request2.Accept = "*/*";
                            request2.Headers[HttpRequestHeader.CacheControl] = "no-cache";
                            //request.Host = "www.youtube-mp3.org";
                            request2.KeepAlive = true;
                            request2.Headers["Cookie"] = "ux=321798f0-d474-11e2-a636-0d375751d22c|0|0|1371324569|1371756569|8f8941e903a8209931810e4f3e186ee7; __utma=120311424.1527317710.1371160626.1371302565.1371321039.13; __utmc=120311424; __utmz=120311424.1371211368.3.2.utmcsr=google|utmccn=(organic)|utmcmd=organic|utmctr=(not%20provided)";
                            request2.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.110 Safari/537.36";
                            request2.Referer = "http://www.youtube-mp3.org/";
                            request2.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate,sdch";
                            request2.Headers[HttpRequestHeader.AcceptLanguage] = "cs-CZ,cs;q=0.8";
                            request2.Headers["Accept-Location"] = "*";
                            request2.BeginGetResponse((c) =>
                            {
                                var stream2 = request2.EndGetResponse(c).GetResponseStream();
                                string response2 = new StreamReader(stream2).ReadToEnd();
                                if (response2 != this.id)
                                    throw ex;

                                ItemInfo info = new ItemInfo();
                                do
                                {
                                    string st = string.Format("http://www.youtube-mp3.org/a/itemInfo/?video_id={0}", id);
                                    request2 = (HttpWebRequest)WebRequest.Create(st);
                                    string response3 = new StreamReader(request2.GetResponse().GetResponseStream()).ReadToEnd();
                                    response3 = response3.Replace("info = ", string.Empty);
                                    response3 = response3.Replace("};", "}");
                                    info = JsonConvert.DeserializeObject<ItemInfo>(response3);
                                }
                                while (info.status != "serving");

                                this.title = info.title;
                                string stringurl = string.Format("http://www.youtube-mp3.org/get?video_id={0}&h={1}", this.id, info.h);
                                WebClient web = new WebClient();
                                string file = Path.GetTempFileName();
                                web.DownloadFile(new Uri(stringurl, UriKind.Absolute), file);
                                youtubeUri.SetUrl(file);
                                if (PlayableReady != null)
                                    PlayableReady(this, EventArgs.Empty);
                            }, request2);
                        }
                        else throw ex;
                    }
                    if (!this.coughtException)
                    {
                        if (PlayableReady != null)
                            PlayableReady(this, EventArgs.Empty);
                    }

                }
                catch { }
            }, request);
        }

        public void InitNew()
        {
            Uri r = new Uri("http://www.youtube.com/get_video_info?&video_id=" + this.id + "&el=detailpage&ps=default&eurl=&gl=US&hl=en");
            WebClient client = new WebClient();
            client.DownloadStringCompleted += (s, e) =>
            {
                var resultArray = e.Result.Split('&');
                string urlMap = string.Empty;
                //string title = string.Empty;
                for (int i = 0; i < resultArray.Length; i++)
                {
                    if (urlMap.IsNotNullOrEmpty() && title.IsNotNullOrEmpty())
                        break;
                    var row = resultArray[i].Split('=');
                    string key = row[0];
                    if (key == "title")
                        this.title = Uri.UnescapeDataString(row[1]).Replace('+', ' ');
                    else if (key == "url_encoded_fmt_stream_map")
                        urlMap = row[1];
                }
                urlMap = Uri.UnescapeDataString(urlMap);
                var urlArray = Regex.Split(urlMap, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                List<YoutubeUri> urls = new List<YoutubeUri>();
                for (int i = 0; i < urlArray.Length; i++)
                {
                    var tempUri = new YoutubeUri();
                    var url = urlArray[i].Split('&');
                    string signature = string.Empty;
                    string unescapedUrl = string.Empty;
                    string fallbackHost = string.Empty;
                    for (int j = 0; j < url.Length; j++)
                    {
                        var urlVar = url[j].Split('=');
                        if (urlVar.Length != 2) continue;
                        var uriKey = urlVar[0];
                        var value = Uri.UnescapeDataString(urlVar[1]);
                        switch (uriKey)
                        {
                            case "url":
                                unescapedUrl = Uri.UnescapeDataString(value);
                                break;
                            case "itag":
                                tempUri.Itag = Int32.Parse(value);
                                break;
                            case "sig":
                                signature = value;
                                break;
                            case "s":
                                signature = value;//makeSignature(value);
                                break;
                            case "type":
                                tempUri.Type = value.Contains("video/mp4") ? value : null;
                                break;
                            case "fallback_host":
                                fallbackHost = value;
                                break;
                            default:
                                break;
                        }
                    }
                    tempUri.SetUrl(unescapedUrl + "&fallback_host=" + fallbackHost + "&signature=" + signature);
                    if (tempUri.IsValid)
                        urls.Add(tempUri);
                }

                this.youtubeUri = urls[urls.Count - 1];

                if (PlayableReady != null)
                    PlayableReady(this, EventArgs.Empty);
            };
            client.DownloadStringAsync(r);
        }

        public async void InitAsync()
        {
            WebClient c = new WebClient();
            string result = await c.DownloadStringTaskAsync(string.Format("http://www.youtube.com/watch?v={0}&nomobile=1", this.id));
            this.title = Uri.UnescapeDataString(Regex.Match(result, "\"title\": \"(.*?)\"").Groups[1].Value).Replace("\\u0026", "&");
            var urlArray = Regex.Match(result, "url_encoded_fmt_stream_map\": \"(.*?)\"").Groups[1].Value.Split(',');

            List<YoutubeUri> urls = new List<YoutubeUri>();
            for (int i = 0; i < urlArray.Length; i++)
            {
                var tempUri = new YoutubeUri();
                var url = urlArray[i].Split(new string[] { "\\u0026" }, StringSplitOptions.None);
                string signature = string.Empty;
                string unescapedUrl = string.Empty;
                string fallbackHost = string.Empty;
                for (int j = 0; j < url.Length; j++)
                {
                    var urlVar = url[j].Split('=');
                    if (urlVar.Length != 2) continue;
                    var uriKey = urlVar[0];
                    var value = Uri.UnescapeDataString(urlVar[1]);
                    switch (uriKey)
                    {
                        case "url":
                            unescapedUrl = Uri.UnescapeDataString(value);
                            break;
                        case "itag":
                            tempUri.Itag = Int32.Parse(value);
                            break;
                        case "sig":
                            signature = value;
                            break;
                        case "s":
                            signature = await MakeSignatureAsync(value);
                            break;
                        case "type":
                            tempUri.Type = value.Contains("video/mp4") ? value : null;
                            break;
                        case "fallback_host":
                            fallbackHost = value;
                            break;
                        default:
                            break;
                    }
                }
                tempUri.SetUrl(unescapedUrl + "&fallback_host=" + fallbackHost + "&signature=" + signature);
                if (tempUri.IsValid)
                    urls.Add(tempUri);
            }

            this.youtubeUri = urls[urls.Count - 1];

            if (PlayableReady != null && !this.isAdded)
            {
                PlayableReady(this, EventArgs.Empty);
                this.isAdded = true;
            }
        }

        public void Init()
        {
            //InitAsync();
            //return;

            this.youtubeUri = new YoutubeUri();
            VideoInfo info = Youtube.GetYoutubeDownloadUrl(this.id);
            this.youtubeUri.SetUrl(info.Url);
            this.title = info.Title;

            if (PlayableReady != null && !this.isAdded)
            {
                PlayableReady(this, EventArgs.Empty);
                this.isAdded = true;
            }

            return;

            #region old code

            /*

            Uri r = new Uri("http://www.youtube.com/watch?v=" + this.id + "&nomobile = 1");
            WebClient client = new WebClient();
            client.DownloadStringCompleted += (s, e) =>
            {
                this.title = Uri.UnescapeDataString(Regex.Match(e.Result, "\"title\": \"(.*?)\"").Groups[1].Value).Replace("\\u0026", "&");
                var urlArray = Regex.Match(e.Result, "url_encoded_fmt_stream_map\": \"(.*?)\"").Groups[1].Value.Split(',');

                List<YoutubeUri> urls = new List<YoutubeUri>();
                for (int i = 0; i < urlArray.Length; i++)
                {
                    var tempUri = new YoutubeUri();
                    var url = urlArray[i].Split(new string[]{"\\u0026"}, StringSplitOptions.None);
                    string signature = string.Empty;
                    string unescapedUrl = string.Empty;
                    string fallbackHost = string.Empty;
                    for (int j = 0; j < url.Length; j++)
                    {
                        var urlVar = url[j].Split('=');
                        if (urlVar.Length != 2) continue;
                        var uriKey = urlVar[0];
                        var value = Uri.UnescapeDataString(urlVar[1]);
                        switch (uriKey)
                        {
                            case "url":
                                unescapedUrl = Uri.UnescapeDataString(value);
                                break;
                            case "itag":
                                tempUri.Itag = Int32.Parse(value);
                                break;
                            case "sig":
                                signature = value;
                                break;
                            case "s":
                                signature = makeSignature(value);
                                break;
                            case "type":
                                tempUri.Type = value.Contains("video/mp4") ? value : null;
                                break;
                            case "fallback_host":
                                fallbackHost = value;
                                break;
                            default:
                                break;
                        }
                    }
                    tempUri.SetUrl(unescapedUrl + "&fallback_host=" + fallbackHost + "&signature=" + signature);
                    if (tempUri.IsValid)
                        urls.Add(tempUri);
                }

                this.youtubeUri = urls[urls.Count - 1];

                if (PlayableReady != null)
                    PlayableReady(this, EventArgs.Empty);
            };
            client.DownloadStringAsync(r);
                      
             */

            #endregion
        }

        private string getTitle(string unescapedResult)
        {
            var array = unescapedResult.Split('&');
            foreach (var key in array)
            {
                var keyArray = key.Split('=');
                if (keyArray[0] == "title")
                    this.title = keyArray[1];
            }
            return this.title;
        }

        public string makeSignature(string signature)
        {
            string sig = signature;
            string signaturePath = string.Empty;
            var engine = new ScriptEngine();
            engine.SetGlobalValue("sig", sig);
            SignatureUpdater signatureUpdater = new SignatureUpdater();
            signatureUpdater.CheckForUpdateCompleted += (s, e) =>
            {
                if (e.NeedsUpdate)
                {
                    signatureUpdater.DownloadUpdateCompleted += (se, ea) =>
                    {
                        signaturePath = ea.SignatureFilePath;
                    };
                    signatureUpdater.DownloadUpdate();
                }
                else
                    signaturePath = e.SignatureFilePath;
            };
            signatureUpdater.CheckForUpdate();

            while (string.IsNullOrEmpty(signaturePath))
                Thread.Sleep(200);

            engine.ExecuteFile(signaturePath);
            sig = engine.GetGlobalValue<string>("sig");

            #region  old signature code

            //switch (signature.Length)
            //{
            //    case 88:
            //        {
            //            sig = sig.Substring(2);
            //            sig = swap(sig, 1);
            //            sig = swap(sig, 10);
            //            sig = sig.Reverse();
            //            sig = sig.Substring(2);
            //            sig = swap(sig, 23);
            //            sig = sig.Substring(3);
            //            sig = swap(sig, 15);
            //            sig = swap(sig, 34);
            //            break;
            //        }
            //    case 87:
            //        {
            //            string[] sigs = sig.Split('.');
            //            string sigA = sigs[1];
            //            string sigB = sigs[0];
            //            sigA = sigA.Substring(0, 40).Reverse();
            //            sigB = sigB.Substring(3, 40).Reverse();
            //            sig = sigA.Substring(21, 1) + sigA.Substring(1, 20) + sigA.Substring(0, 1) + sigA.Substring(22, 9) + sig.Substring(0, 1) + sigA.Substring(32, 8) + "." + sigB;
            //            break;
            //        }
            //    case 86:
            //        {
            //            sig = sig.Substring(2, 15) + sig.Substring(0, 1) + sig.Substring(18, 23) + sig.Substring(79, 1) + sig.Substring(42, 1) + sig.Substring(43, 36) + sig.Substring(82, 1) + sig.Substring(80, 2) + sig.Substring(41, 1);
            //            break;
            //        }
            //    case 85:
            //        {
            //            string sigA = sig.Substring(44, 40).Reverse();
            //            string sigB = sig.Substring(3, 40).Reverse();
            //            sig = sigA.Substring(7, 1) + sigA.Substring(1, 6) + sigA.Substring(0, 1) + sigA.Substring(8, 15) + sig.Substring(0, 1) + sigA.Substring(24, 9) + sig.Substring(1, 1) + sigA.Substring(34, 6) + sig.Substring(43, 1) + sigB;
            //            break;
            //        }
            //    case 84:
            //        {
            //            string sigA = sig.Substring(44, 40).Reverse();
            //            string sigB = sig.Substring(3, 40).Reverse();
            //            sig = sigA + sig.Substring(43, 1) + sigB.Substring(0, 6) + sig.Substring(2, 1) + sigB.Substring(7, 9) + sigB.Substring(39, 1) + sigB.Substring(17, 22) + sigB.Substring(16, 1); 
            //            break;
            //        }
            //    case 83:
            //        {
            //            string sigA = sig.Substring(43, 40).Reverse();
            //            string sigB = sig.Substring(2, 40).Reverse();
            //            sig = sigA.Substring(30, 1) + sigA.Substring(1, 26) + sigB.Substring(39, 1) + sigA.Substring(28, 2) + sigA.Substring(0, 1) + sigA.Substring(31, 9) + sig.Substring(42, 1) + sigB.Substring(0, 5) + sigA.Substring(27, 1) + sigB.Substring(6, 33) + sigB.Substring(5, 1);
            //            break;
            //        }
            //    case 82:
            //        {
            //            string sigA = sig.Substring(34, 48).Reverse();
            //            string sigB = sig.Substring(0, 33).Reverse();
            //            sig = sigA.Substring(45, 1) + sigA.Substring(2, 12) + sigA.Substring(0, 1) + sigA.Substring(15, 26) + sig.Substring(33, 1) + sigA.Substring(42, 1) + sigA.Substring(43, 1) + sigA.Substring(44, 1) + sigA.Substring(41, 1) + sigA.Substring(46, 1) + sigB.Substring(32, 1) + sigA.Substring(14, 1) + sigB.Substring(0, 32) + sigA.Substring(47, 1); 
            //            break;
            //        }
            //    default:
            //        throw new ArgumentException("Invalid signature!");
            //}

            #endregion

            return sig;
        }

        public async Task<string> MakeSignatureAsync(string signature)
        {
            string signaturePath = string.Empty;
            var engine = new ScriptEngine();
            engine.SetGlobalValue("sig", signature);
            SignatureUpdater signatureUpdater = new SignatureUpdater();
            var args = await signatureUpdater.CheckForUpdateAsync();
            if (args.NeedsUpdate)
            {
                args = await signatureUpdater.DownloadUpdateAsync();
                signaturePath = args.SignatureFilePath;
            }
            else
                signaturePath = args.SignatureFilePath;

            engine.ExecuteFile(signaturePath);
            return engine.GetGlobalValue<string>("sig");
        }

        private string swap(string s, int n)
        {
            var array = s.ToCharArray();
            var c = array[0];
            array[0] = array[n % array.Length];
            array[n] = c;
            return new string(array);
        }

        public void Play()
        {
            while (youtubeUri == null)
                Thread.Sleep(100);

            if (this.state == DriftPlayer.PlaybackState.Playing) return;
            if (this.state == DriftPlayer.PlaybackState.Stopped)
            {
                if (youtubeUri.IsExpired)
                {
                    Console.WriteLine("{0} with id {1} is expired.", this.title, this.id);
                    Init();
                }
                    
                if (coughtException)
                    if (!File.Exists(youtubeUri.Uri.AbsolutePath))
                        Init();

                this.waveOut = new WaveOutEvent();
                waveOut.PlaybackStopped += waveOut_PlaybackStopped;
                this.reader = new MediaFoundationReader(this.youtubeUri.Uri.AbsoluteUri);
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
                if (this.coughtException)
                    File.Delete(this.youtubeUri.Uri.AbsolutePath);
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

        private void playMp3FromStream(string url)
        {

            #region crap
            //using (Stream ms = new MemoryStream())
            //{
            //    using (Stream stream = WebRequest.Create(url)
            //        .GetResponse().GetResponseStream())
            //    {
            //        byte[] buffer = new byte[32768];
            //        int read;
            //        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            //        {
            //            ms.Write(buffer, 0, read);
            //        }
            //    }

                //var request = WebRequest.Create(url);
                //request.BeginGetResponse((c) => 
                //{
                //    using (Stream ms = new MemoryStream())
                //    {
                //        using (Stream stream = request.EndGetResponse(c).GetResponseStream())
                //        {
                //            byte[] buffer = new byte[32768];
                //            int read;
                //            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                //            {
                //                ms.Write(buffer, 0, read);
                //            }
                //        }
                //        ms.Position = 0;
                //        using (WaveStream blockAlignedStream = new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(ms))))
                //        {
                //            this.volumeProvider = new VolumeSampleProvider(blockAlignedStream.ToSampleProvider());
                //            this.waveOut.Init(this.volumeProvider);
                //            this.volumeProvider.Volume = this.volume;
                //            this.isInit = true;
                //            this.waveOut.Play();
                //            while (waveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                //            {
                //                System.Threading.Thread.Sleep(100);
                //            }
                //        }
                //    }
                //}, request);

                //ms.Position = 0;
                //using (WaveStream blockAlignedStream = new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(ms))))
                //{
                //    this.volumeProvider = new VolumeSampleProvider(blockAlignedStream.ToSampleProvider());
                //    this.waveOut.Init(this.volumeProvider);
                //    this.volumeProvider.Volume = this.volume;
                //    this.isInit = true;
                //    this.waveOut.Play();
                //    while (waveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                //    {
                //        System.Threading.Thread.Sleep(100);
                //    }
                //}
            //}
            #endregion

            WebClient web = new WebClient();
            string file = Path.GetTempFileName();
            web.DownloadFileCompleted += (s, e) =>
            {
                this.reader = new MediaFoundationReader(file);
                this.volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider());
                this.waveOut.Init(volumeProvider);
                this.volumeProvider.Volume = this.volume;
                this.isInit = true;
                this.waveOut.Play();

                if (PlayableReady != null)
                    PlayableReady(this, EventArgs.Empty);
            };
            web.DownloadFileAsync(this.youtubeUri.Uri, file);
            
        }

        public YoutubeAudio(SerializationInfo info, StreamingContext ctxt)
        {
            this.isAdded = true;
            this.title = (string)info.GetValue("Title", typeof(string));
            this.youtubeUri = (YoutubeUri)info.GetValue("URI", typeof(YoutubeUri));
            this.id = (string)info.GetValue("ID", typeof(string));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("URI", this.youtubeUri, typeof(YoutubeUri));
            info.AddValue("Title", this.title);
            info.AddValue("ID", this.id);

            //info.AddValue("ExprireDate", this.youtubeUri.ExpireDate);
            //info.AddValue("Itag", this.youtubeUri.Itag);
            //info.AddValue("Type", this.youtubeUri.Type);
            //info.AddValue("Url", this.youtubeUri.Url);
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
    }
}
