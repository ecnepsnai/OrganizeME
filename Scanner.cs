namespace OrganizeME
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Scanner
    {
        public readonly string scanRoot;
        public readonly string organizedRoot;
        public event EventHandler<Progress> ProgressUpdate;
        private List<Media> media;
        private int total;
        private int current = 0;
        private object semaphore = new object();

        public Scanner(string scanRoot, string organizedRoot)
        {
            this.scanRoot = scanRoot;
            this.organizedRoot = organizedRoot;
        }

        public List<Media> Scan()
        {
            this.media = new List<Media>();

            var allFiles = Directory.EnumerateFiles(this.scanRoot, "*.*", SearchOption.AllDirectories).ToArray();
            this.total = allFiles.Length;
            this.ProgressUpdate.Invoke(this, new Progress(0, this.total));

            var tasks = allFiles.Select(processFile);
            Task.WaitAll(tasks.ToArray());
            return media;
        }

        private Task processFile(string filePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    var media = new Media(filePath, this.organizedRoot);
                    lock (this.semaphore)
                    {
                        this.media.Add(media);
                    }
                }
                catch
                {
                    //
                }
                finally
                {
                    lock (this.semaphore)
                    {
                        this.current++;
                        this.ProgressUpdate.Invoke(this, new Progress(this.current, this.total));
                    }
                }
            });
        }
    }
}
