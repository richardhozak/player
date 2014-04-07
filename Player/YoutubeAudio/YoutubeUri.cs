using System;
using System.Runtime.Serialization;

namespace DriftPlayer
{
    [Serializable()]
    public class YoutubeUri : ISerializable
    {
        public string Url { get; private set; }

        internal void SetUrl(string url)
        {
            this.Url = url;
            this.Url = url;
            int index = this.Url.IndexOf("expire=");
            if (index != -1)
            {
                string str = this.Url.Substring(index + 7, 10);
                double timestamp = double.Parse(str);
                this.ExpireDate = timestamp.ToDateTime();
            }
            else
                throw new Exception("Not a valid url");//this.ExpireDate = DateTime.Now.AddYears(1);
        }

        public Uri Uri { get { return new Uri(Url, UriKind.Absolute); } }
        public int Itag { get; set; }
        public string Type { get; set; }
        public DateTime ExpireDate { get; private set; }

        public bool IsValid
        {
            get
            {
                return true;
            }
            //get { return Url != null && Itag > 0 && Type != null; }
        }

        public bool IsExpired
        {
            //get
            //{
            //    return false;
            //}
            get { return this.ExpireDate < DateTime.Now.Subtract(TimeSpan.FromMinutes(30)); }
        }

        public YoutubeUri(SerializationInfo info, StreamingContext ctxt)
        {
            this.Url = (string)info.GetValue("Url", typeof(string));
            this.Itag = (int)info.GetValue("Itag", typeof(int));
            this.Type = (string)info.GetValue("Type", typeof(string));
            this.ExpireDate = (DateTime)info.GetValue("ExpireDate", typeof(DateTime));
        }

        public YoutubeUri()
        {
            // TODO: Complete member initialization
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ExpireDate", this.ExpireDate);
            info.AddValue("Itag", this.Itag);
            info.AddValue("Type", this.Type);
            info.AddValue("Url", this.Url);
        }
    }
}
