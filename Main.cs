namespace OrganizeME
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Windows.Threading;
    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    public partial class Main : Form
    {
        private string mediaRoot = null;
        private List<Media> media;
        private Dispatcher uiDispatcher;

        public Main()
        {
            InitializeComponent();
            uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        private string GetMediaRoot()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                IsFolderPicker = true,
                Title = "Select Directory Containing Media"
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                Application.Exit();
            }

            return dialog.FileName;
        }

        private void Main_Activated(object sender, EventArgs e)
        {
            if (this.mediaRoot == null)
            {
                startOrganize();
            }
        }

        private async void startOrganize()
        {
            this.mediaRoot = GetMediaRoot();
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            await ScanAsync();

            if (this.media == null || this.media.Count() == 0)
            {
                MessageBox.Show("No media files found in selected directory", "OrganizeME", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }

            AddLogLine($"Found {this.media.Count()} media items to organize");

            await OrganizeAsync();
            await ConvertAsync();

            AddLogLine($"Organized {this.media.Count()} media files");
            MessageBox.Show($"Organized {this.media.Count()} media files", "OrganizeME", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        Task ScanAsync()
        {
            var scanner = new Scanner(this.mediaRoot, Path.Combine(this.mediaRoot, "Organized"));
            scanner.ProgressUpdate += (s, progress) => this.uiDispatcher.Invoke(() =>
                {
                    UpdateProgress("Scanning", progress.Current, progress.Total);
                });
            return Task.Run(() => {
                this.media = scanner.Scan();
            });
        }

        Task OrganizeAsync()
        {
            var organizer = new Organizer(this.media);
            organizer.ProgressUpdate += (s, progress) => this.uiDispatcher.Invoke(() =>
            {
                UpdateProgress("Organizing", progress.Current, progress.Total);
                if (progress.LogLine != null)
                {
                    AddLogLine(progress.LogLine);
                }
            });
            return Task.Run(() => organizer.Organize());
        }

        Task ConvertAsync()
        {
            var converter = new Converter(this.media);
            converter.ProgressUpdate += (s, progress) => this.uiDispatcher.Invoke(() =>
            {
                UpdateProgress("Encoding", progress.Current, progress.Total);
                if (progress.LogLine != null)
                {
                    AddLogLine(progress.LogLine);
                }
            });
            return Task.Run(() => converter.Convert());
        }

        private void UpdateProgress(string labelText, int current, int total)
        {
            label.Text = labelText + " (" + current + "/" + total + ")";
            progressBar.Value = current;
            progressBar.Maximum = total;
            progressBar.Style = ProgressBarStyle.Continuous;
            TaskbarManager.Instance.SetProgressValue(current, total);
        }

        private void AddLogLine(string logLine)
        {
            this.logBox.Items.Add(logLine);
            this.logBox.Update();
        }
    }
}
