namespace OrganizeME
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class Organizer
    {
        public readonly List<Media> media;
        public event EventHandler<Progress> ProgressUpdate;

        public Organizer(List<Media> media)
        {
            this.media = media;
        }

        public void Organize()
        {
            var total = this.media.Count;
            var current = 0;
            this.ProgressUpdate.Invoke(this, new Progress(current, total));

            foreach (Media media in this.media)
            {
                current++;
                Directory.CreateDirectory(media.NewPath);

                var i = 1;
                while (File.Exists(media.NewFilePath()))
                {
                    media.SetIndex(i);
                    i++;
                }

                File.Move(media.OriginalFilePath(), media.NewFilePath());
                this.ProgressUpdate.Invoke(this, new Progress(current, total, $"Moved {media.OriginalFilePath()} to {media.NewFilePath()}"));
            }
        }
    }
}
