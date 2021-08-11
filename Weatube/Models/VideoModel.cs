using System.Windows.Media;
using System.Collections.ObjectModel;
using Weatube.Properties;
using Weatube.Viewmodels;
using System.Windows;
using System.Diagnostics;

namespace Weatube.Models
{
    class VideoModel : NotifyPropertyChangedBehavior
    {
        public YoutubeDL.Video YoutubeVideo { get; set; }

        public string Name { get { return YoutubeVideo?.Name; } }

        public ImageSource Image { get { return YoutubeVideo?.ImageSourceFromBitmap(); } }

        public string Type { get { return YoutubeVideo?.Type.ToUpper(); } }

        public bool _IsDownloaded = false;
        public bool IsDownloaded
        {
            get
            {
                return _IsDownloaded;
            }
            set
            {
                if (value && DownloadProcess != null && !DownloadProcess.HasExited) DownloadProcess.Kill();
                if (!value) DownloadState = Utils.DownloadStateChange(0d);
                _IsDownloaded = value;
            }
        }

        public string SavePath { get; private set; }

        public ObservableCollection<YoutubeDL.Video.OutputFormat> outputFormats { get; set; }

        public Process DownloadProcess { get; set; }

        public YoutubeDL.Video.OutputFormat selectedFormat
        {
            get
            {
                return YoutubeVideo?.SelectedFormat;
            }
            set
            {
                if (YoutubeVideo != null) { YoutubeVideo.SelectedFormat = value; IsDownloaded = false; }
            }
        }

        public LinearGradientBrush _DownloadState { get; set; }

        public LinearGradientBrush DownloadState 
        {
            get { return _DownloadState; }

            set { if (value != null) _DownloadState = value; }
        }

        public bool IsPanelEnabled { get; set; }

        public Thickness Margin { get; set; }

        public VideoModel(YoutubeDL.Video vid)
        {
            IsPanelEnabled = false;
            YoutubeVideo = vid;
            outputFormats = new ObservableCollection<YoutubeDL.Video.OutputFormat>(vid.AvailableFormats);
            DownloadState = Utils.DownloadStateChange(0d);
            Margin = new Thickness(-1600, 0, 0, 0);
        }

        public void Disable()
        {
            System.Console.WriteLine("Disabling");
            IsDownloaded = true;
            System.Console.WriteLine(DownloadProcess?.ExitCode);
        }

        public void DownloadStateChange(string data)
        {
            if (data == null) return;
            if (data.Contains("Destination:")) SavePath = data.Split(new char[] { ':' }, 2)[1].Trim();
            else
            {
                var pc = Utils.PercentFromOutput(data);
                DownloadState = Utils.DownloadStateChange(pc);
            }
        }
    }
}