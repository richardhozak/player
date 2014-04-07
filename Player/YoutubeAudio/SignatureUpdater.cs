using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DriftPlayer
{
    class SignatureUpdater
    {
        private const string sourceUrl = "http://userscripts.org/scripts/source/25105.user.js";
        private const string updateUrl = "http://userscripts.org/scripts/source/25105.meta.js";
        private bool needsUpdate;
        public event EventHandler<UpdateEventArgs> CheckForUpdateCompleted;
        public event EventHandler<UpdateEventArgs> DownloadUpdateCompleted;
        private FileInfo signatureFile;

        public SignatureUpdater()
        {
            this.needsUpdate = false;
            string signatureDir = System.AppDomain.CurrentDomain.BaseDirectory + "signature.js";
            signatureFile = new FileInfo(signatureDir);
        }

        public void CheckForUpdate()
        {
            //pokud nekdo zmenil soubor, je potreba update
            //this.needsUpdate = this.signatureFile.Exists ? Properties.Settings.Default.LastSignatureUpdate != this.signatureFile.LastWriteTime : true;
            //int l = DateTime.Compare(Properties.Settings.Default.LastSignatureUpdate, this.signatureFile.LastWriteTime);
            //string o = Properties.Settings.Default.LastSignatureUpdate.ToString();
            this.needsUpdate = this.signatureFile.Exists ? Properties.Settings.Default.LastSignatureUpdate.ToString() != this.signatureFile.LastWriteTime.ToString() : true;
            if (this.needsUpdate)
            {
                if (CheckForUpdateCompleted != null)
                    CheckForUpdateCompleted(this, new UpdateEventArgs(this.needsUpdate, this.signatureFile));
                return;
            }
            if (DateTime.Now.Subtract(Properties.Settings.Default.LastCheckForUpdate) < TimeSpan.FromHours(12))
            {
                if (CheckForUpdateCompleted != null)
                    CheckForUpdateCompleted(this, new UpdateEventArgs(this.signatureFile));
                return;
            }
            Properties.Settings.Default.LastCheckForUpdate = DateTime.Now;
            Properties.Settings.Default.Save();
            WebClient c = new WebClient();
            c.DownloadStringCompleted += (s, e) =>
            {
                using (StringReader reader = new StringReader(e.Result))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("// @version"))
                        {
                            //pokud je nova verze je potreba update
                            this.needsUpdate = Properties.Settings.Default.SignatureVersion != line.Substring(12);
                            break;
                        }
                    }
                }
                if (CheckForUpdateCompleted != null)
                    CheckForUpdateCompleted(this, new UpdateEventArgs(this.needsUpdate, this.signatureFile));
            };
            c.DownloadStringAsync(new Uri(updateUrl));
        }

        public async Task<UpdateEventArgs> CheckForUpdateAsync()
        {
            return new UpdateEventArgs(false, this.signatureFile);

            //TODO
            this.needsUpdate = this.signatureFile.Exists ? Properties.Settings.Default.LastSignatureUpdate.ToString() != this.signatureFile.LastWriteTime.ToString() : true;
            if (this.needsUpdate)
                return new UpdateEventArgs(this.needsUpdate, this.signatureFile);
            if (DateTime.Now.Subtract(Properties.Settings.Default.LastCheckForUpdate) < TimeSpan.FromHours(12))
                return new UpdateEventArgs(this.signatureFile);
            Properties.Settings.Default.LastCheckForUpdate = DateTime.Now;
            Properties.Settings.Default.Save();
            WebClient c = new WebClient();
            string result = await c.DownloadStringTaskAsync(updateUrl);
            using (StringReader reader = new StringReader(result))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("// @version"))
                    {
                        //pokud je nova verze je potreba update
                        this.needsUpdate = Properties.Settings.Default.SignatureVersion != line.Substring(12);
                        break;
                    }
                }
            }
            return new UpdateEventArgs(this.needsUpdate, this.signatureFile);
        }

        public async Task<UpdateEventArgs> DownloadUpdateAsync()
        {
            WebClient c = new WebClient();
            string result = await c.DownloadStringTaskAsync(sourceUrl);
            string function = string.Empty;
            string version = string.Empty;
            using (StringReader reader = new StringReader(result))
            {
                string line;
                bool functionReadStarted = false;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("// @version"))
                        version = line.Substring(12);
                    if (functionReadStarted)
                    {
                        if (line.Contains("return sig;"))
                            break;

                        function += line;
                    }
                    if (line.Contains("function decryptSignature(sig)"))
                        functionReadStarted = true;
                }
            }
            //function = function.ExceptBlanks();
            function.Replace("};", "};\n").Replace(");", ");\n");
            try
            {
                if (!this.signatureFile.Exists)
                    this.signatureFile.Create().Dispose();
                using (TextWriter tw = new StreamWriter(this.signatureFile.Open(FileMode.Truncate)))
                {
                    tw.Write(function);
                }
                Properties.Settings.Default.SignatureVersion = version;
                this.signatureFile.Refresh();
                Properties.Settings.Default.LastSignatureUpdate = this.signatureFile.LastWriteTime;
                Properties.Settings.Default.Save();
            }
            catch { }
            return new UpdateEventArgs(this.signatureFile);
        }

        public void DownloadUpdate()
        {
            WebClient c = new WebClient();
            c.DownloadStringCompleted += (s, e) => 
            {
                string function = string.Empty;
                string version = string.Empty;
                using (StringReader reader = new StringReader(e.Result))
                {
                    string line;
                    bool functionReadStarted = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("// @version"))
                            version = line.Substring(12);
                        if (functionReadStarted)
                        {
                            if (line.Contains("return sig;"))
                                break;

                            function += line;
                        }
                        if (line.Contains("function decryptSignature(sig)"))
                            functionReadStarted = true;
                    }
                }
                function.Replace("};", "};\n").Replace(");", ");\n");
                try
                {
                    if (!this.signatureFile.Exists)
                        this.signatureFile.Create().Dispose();
                    using (TextWriter tw = new StreamWriter(this.signatureFile.Open(FileMode.Truncate)))
                    {
                        tw.Write(function);
                    }
                    Properties.Settings.Default.SignatureVersion = version;
                    this.signatureFile.Refresh();
                    Properties.Settings.Default.LastSignatureUpdate = this.signatureFile.LastWriteTime;
                    Properties.Settings.Default.Save();
                }
                catch { }
                if (DownloadUpdateCompleted != null)
                    DownloadUpdateCompleted(this, new UpdateEventArgs(this.signatureFile));
            };
            c.DownloadStringAsync(new Uri(sourceUrl));
        }
    }
}
