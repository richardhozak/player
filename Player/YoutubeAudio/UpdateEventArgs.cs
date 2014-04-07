using System.IO;

namespace DriftPlayer
{
    public class UpdateEventArgs
    {
        public UpdateEventArgs(bool needsUpdate, FileInfo fi)
        {
            this.NeedsUpdate = needsUpdate;
            this.SignatureFilePath = needsUpdate ? string.Empty : (fi as FileSystemInfo).FullName;
        }

        public UpdateEventArgs(FileInfo fi)
        {
            this.NeedsUpdate = false;
            this.SignatureFilePath = (fi as FileSystemInfo).FullName;
        }

        public bool NeedsUpdate { get; private set; }
        public string SignatureFilePath { get; private set; }
    }
}
