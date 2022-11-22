namespace OrganizeME
{
    using Microsoft.WindowsAPICodePack.Shell;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    internal class Media
    {
        /// <summary>
        /// The original file name, without directory
        /// </summary>
        public readonly string OriginalFileName;

        /// <summary>
        /// The new file name, without directory
        /// </summary>
        public string NewFileName;

        /// <summary>
        /// The original parent directory, without file name
        /// </summary>
        public readonly string OriginalPath;

        /// <summary>
        /// The new parent directory, without file name
        /// </summary>
        public string NewPath;

        private readonly string[] acceptedExtensions = { "jpg", "jpeg", "png", "gif", "mov", "mp4", "heic", "heif", "hevc", "webp", "dng" };

        public Media(string filePath, string organizedRoot)
        {
            var nameParts = filePath.Split('.');
            var extension = nameParts.Last().ToLower();

            if (!acceptedExtensions.Contains(extension.ToLower()))
            {
                throw new ArgumentException("filePath is not media");
            }
            

            var properties = ShellObject.FromParsingName(filePath).Properties;

            var dateEncodedD = properties.System.Media.DateEncoded;
            var dateCreatedD = properties.System.Photo.DateTaken;
            var dateModifiedD = properties.System.DateModified;

            if (dateEncodedD.Value == null && dateCreatedD.Value == null && dateModifiedD.Value == null)
            {
                throw new ArgumentException("cant find good date");
            }

            var oldestDate = DateTime.Now;
            if (dateEncodedD.Value != null)
            {
                if (dateEncodedD.Value < oldestDate)
                {
                    oldestDate = (DateTime)dateEncodedD.Value;
                }
            }
            if (dateCreatedD.Value != null)
            {
                if (dateCreatedD.Value < oldestDate)
                {
                    oldestDate = (DateTime)dateCreatedD.Value;
                }
            }
            if (dateModifiedD.Value != null)
            {
                if (dateModifiedD.Value < oldestDate)
                {
                    oldestDate = (DateTime)dateModifiedD.Value;
                }
            }

            var originalFilePath = Path.GetDirectoryName(filePath);
            var originalFileName = Path.GetFileName(filePath);

            string newFileName = oldestDate.ToString("yyyy-MM-dd HH.mm.ss") + "." + extension;
            string newParentDir = Path.Combine(organizedRoot, oldestDate.ToString("yyyy"), oldestDate.ToString("M-MMM"));

            this.OriginalFileName = originalFileName;
            this.NewFileName = newFileName;
            this.OriginalPath = originalFilePath;
            this.NewPath = newParentDir;
            return;
        }

        public string OriginalFilePath()
        {
            return Path.Combine(this.OriginalPath, this.OriginalFileName);
        }

        public string NewFilePath()
        {
            return Path.Combine(this.NewPath, this.NewFileName);
        }

        public void SetIndex(int index)
        {
            var nameParts = this.NewFileName.Split('.');
            var extension = nameParts.Last().ToLower();
            this.NewFileName = this.NewFileName.Replace("." + extension, "-" + index + "." + extension);
        }

        public void SetExtension(string extension)
        {
            var nameParts = this.NewFileName.Split('.');
            var oldExtension = nameParts.Last().ToLower();
            this.NewFileName = this.NewFileName.Replace("." + oldExtension, "." + extension);
        }

        public override string ToString()
        {
            return this.NewFileName;
        }
    }
}
