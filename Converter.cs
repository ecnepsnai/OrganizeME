namespace OrganizeME
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    internal class Converter
    {
        public readonly List<Media> mediaToConvert;
        public event EventHandler<Progress> ProgressUpdate;
        private readonly string ConvertExe;
        private readonly string IdentifyExe;
        private readonly object semaphore = new object();
        private int currentMediaIdx = 0;
        private int convertedMediaCount = 0;

        private readonly Dictionary<string, string> extensionsToFormat = new Dictionary<string, string>()
            {
                { "jpg", "JPEG" },
                { "jpeg", "JPEG" },
                { "heif", "HEIC" },
                { "heic", "HEIC" },
                { "png", "PNG" }
            };

        private readonly Dictionary<string, string> formatToExtension = new Dictionary<string, string>()
            {
                { "JPEG", "jpg" },
                { "HEIC", "heic" },
                { "PNG", "png" }
            };

        public Converter(List<Media> media)
        {
            this.mediaToConvert = media;
            string rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            this.ConvertExe = Path.Combine(rootPath, "convert.exe");
            if (this.ConvertExe == null)
            {
                throw new FileNotFoundException("convert.exe not found");
            }
            this.IdentifyExe = Path.Combine(rootPath, "identify.exe");
            if (this.IdentifyExe == null)
            {
                throw new FileNotFoundException("identify.exe not found");
            }
        }

        public void Convert()
        {
            var total = this.mediaToConvert.Count;
            this.convertedMediaCount = 0;
            this.ProgressUpdate.Invoke(this, new Progress(this.convertedMediaCount, total));

            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threads.Add(new Thread(ConvertNextMedia));
            }

            foreach (Thread thread in threads)
            {
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            this.ProgressUpdate.Invoke(this, new Progress(this.mediaToConvert.Count, this.mediaToConvert.Count));
        }

        private void ConvertNextMedia()
        {
            while (true)
            {
                Media nextMedia;
                lock (this.semaphore)
                {
                    if (this.currentMediaIdx >= this.mediaToConvert.Count)
                    {
                        return;
                    }

                    nextMedia = this.mediaToConvert[this.currentMediaIdx];
                    this.currentMediaIdx++;
                }

                bool didConvert = false;
                string format = FixIncorrectExtension(nextMedia);
                if (format == "HEIC")
                {
                    ConvertHeicToJpeg(nextMedia);
                    didConvert = true;
                }

                lock (this.semaphore)
                {
                    this.convertedMediaCount++;
                    string message = null;
                    if (didConvert)
                    {
                        message = $"Converted {nextMedia.NewFileName} to {nextMedia.NewFileName.Replace(".heic", ".jpg")}";
                    }
                    this.ProgressUpdate.Invoke(this, new Progress(this.convertedMediaCount, this.mediaToConvert.Count, message));
                }
            }
        }

        private string FixIncorrectExtension(Media media)
        {
            var nameParts = media.NewFileName.Split('.');
            var extension = nameParts.Last().ToLower();

            if (extensionsToFormat.ContainsKey(extension))
            {
                var format = GetMediaFormat(media);
                if (extensionsToFormat[extension] != format)
                {
                    var oldPath = media.NewFilePath();
                    media.SetExtension(formatToExtension[format]);
                    var newPath = media.NewFilePath();
                    File.Move(oldPath, newPath);
                }

                return format;
            }

            return "";
        }

        private string GetMediaFormat(Media media)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = this.IdentifyExe;
            p.StartInfo.WorkingDirectory = media.NewPath;
            p.StartInfo.Arguments = $"\"{media.NewFileName}\"";
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                throw new Exception("Error identifying file: " + stderr);
            }
            if (stdout.Length == 0)
            {
                throw new Exception("Error identifying file: " + stdout);
            }
            stdout = stdout.Replace(media.NewFileName + " ", "");
            var parts = stdout.Split(' ');
            return parts[0];
        }

        private void ConvertHeicToJpeg(Media media)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = this.ConvertExe;
            p.StartInfo.WorkingDirectory = media.NewPath;
            p.StartInfo.Arguments = $"\"{media.NewFileName}\" \"{media.NewFileName.Replace(".heic", ".jpg")}\"";
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) {
                throw new Exception("Error converting file: " + stderr);
            }
            File.Delete(media.NewFilePath());
            media.SetExtension(".jpg");
        }
    }
}
