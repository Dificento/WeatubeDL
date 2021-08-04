using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Weatube.Viewmodels;

namespace Weatube.Models
{
    class SuggestionModel : NotifyPropertyChangedBehavior
    {
        public IList<YoutubeDL.Video.OutputFormat> SuggestedTypes { get; set; }

        public YoutubeDL.Video.OutputFormat SelectedType { get; set; }

        public List<YoutubeDL.Video> SelectedVideos { get; set; }

        public ImageSource SuggestedImage { get; set; }

        public string SuggestedVideoName { get; set; }

        public bool IsEnabled { get; set; }

        public SuggestionModel(YoutubeDL ydl)
        {
            SelectedVideos = ydl.VideoList.ToList();
            SuggestedImage = ydl.ImageSourceFromBitmap();
            SuggestedVideoName = ydl.Name;
            SuggestedTypes = ydl.Type == YoutubeDL.YoutubeDLRequestType.YOUTUBEDL_REQUEST_TYPE_SINGLE
          ? ydl.VideoList[0].AvailableFormats : new List<YoutubeDL.Video.OutputFormat>() { ydl.VideoList[0].SelectedFormat };
            SelectedType = ydl.VideoList[0].SelectedFormat;
            IsEnabled = true;
        }

        public SuggestionModel()
        {
            IsEnabled = false;
        }

        public void Disable()
        {
            this.IsEnabled = false;
        }
    }
}
