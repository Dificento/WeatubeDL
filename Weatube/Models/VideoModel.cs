using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Weatube.Properties;
using Weatube.Viewmodels;
using System.Windows;

namespace Weatube.Models
{
    class VideoModel : RootViewModel
    {
        public YoutubeDL.Video YoutubeVideo { get; set; }

        public string Name { get { return YoutubeVideo != null ? YoutubeVideo.Name : null; } }

        public ImageSource Image { get { return YoutubeVideo != null ? YoutubeVideo.ImageSourceFromBitmap() : null; } }

        public ObservableCollection<YoutubeDL.Video.OutputFormat> outputFormats { get; set; }

        public YoutubeDL.Video.OutputFormat selectedFormat
        {
            get
            {
                return YoutubeVideo != null ? YoutubeVideo.SelectedFormat : null;
            }
            set
            {
                if (YoutubeVideo != null) YoutubeVideo.SelectedFormat = value;
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

    }
}
