using DevExpress.Mvvm;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Weatube.Models;
using Weatube.Properties;

namespace Weatube.Viewmodels
{
    class WeatubeViewModel : RootViewModel
    {
        public ObservableCollection<VideoModel> QueuedVideos { get; set; }

        public List<YoutubeDL.Video> SelectedVideos { get; set; }

        private string _SearchVideo { get; set; }

        public ImageSource SuggestedImage { get; set; }

        public string SaveDirectoryPath { get; set; }

        public string SuggestedVideoName { get; set; }

        public bool IsSuggestionEnabled { get; set; }

        public IList<YoutubeDL.Video.OutputFormat> SuggestedTypes { get; set; }

        public YoutubeDL.Video.OutputFormat SelectedType { get; set; }

        public string SearchVideo
        {
            get => _SearchVideo;
            set
            {
                _SearchVideo = value;
                _ = Task.Run(async () =>
                  {
                      Console.WriteLine("Started shit.");
                      async Task<bool> UserKeepsTyping()
                      {
                          string txt = _SearchVideo;
                          await Task.Delay(500);
                          return txt != _SearchVideo;
                      }
                      if (await UserKeepsTyping()) return;
                      if (_SearchVideo.Length < 1)
                      {
                          IsSuggestionEnabled = false;
                          await Task.Delay(300);
                          SelectedVideos = null;
                          SuggestedImage = null;
                          SuggestedVideoName = null;
                          SuggestedTypes = null;
                          SelectedType = null;
                          return;
                      }
                      var vid = new YoutubeDL(_SearchVideo);
                      if (await vid.InitAsync() != null)
                      {
                          SelectedVideos = vid.VideoList.ToList();
                          SuggestedImage = vid.ImageSourceFromBitmap();
                          SuggestedVideoName = vid.Name;
                          SuggestedTypes = vid.Type == YoutubeDL.YoutubeDLRequestType.YOUTUBEDL_REQUEST_TYPE_SINGLE
                        ? vid.VideoList[0].AvailableFormats : new List<YoutubeDL.Video.OutputFormat>() { vid.VideoList[0].SelectedFormat };
                          SelectedType = vid.VideoList[0].SelectedFormat;
                          IsSuggestionEnabled = true;
                          Console.WriteLine("Ended shit.");
                      }
                  });
            }
        }

        public WeatubeViewModel()
        {
            QueuedVideos = new ObservableCollection<VideoModel>();
            SaveDirectoryPath = System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), "Downloads");
        }

        public ICommand AddVideo => 
            new DelegateCommand<List<YoutubeDL.Video>>(async (videos) =>
                {
                    foreach (var video in videos)
                    {
                        video.SelectedFormat = SelectedType;
                        QueuedVideos.Add(new VideoModel(video));
                        await Task.Delay(5);
                        QueuedVideos.Last().IsPanelEnabled = true;
                    }
                    SearchVideo = "";
                }, (videos) => SuggestedVideoName != null && SearchVideo.Length > 1);

        public ICommand DeleteVideo =>
            new DelegateCommand<VideoModel>(async (video) =>
            {
                QueuedVideos[QueuedVideos.IndexOf(video)].IsPanelEnabled = false;
                await Task.Delay(300);
                QueuedVideos.Remove(video);
            }, (video) => QueuedVideos.Contains(video));

        public ICommand DownloadVideos =>
            new DelegateCommand(async () =>
            {
            for (int i = 0; i < QueuedVideos.Count; i++)
                {
                    var process = YoutubeDL.RunProcess(QueuedVideos[i].YoutubeVideo.GetCommandArguments(
                        SaveDirectoryPath + '\\' + QueuedVideos[i].YoutubeVideo.Name + 
                        (QueuedVideos[i].YoutubeVideo.SelectedFormat.Name == "mp3" ? ".mp3" : ".mp4")));
                    while (!process.HasExited)
                        QueuedVideos[i].DownloadState =
                        Utils.DownloadStateChange(Utils.PercentFromOutput(await process.StandardOutput.ReadLineAsync()));
                }
            }, () => QueuedVideos.Count > 0);

        public ICommand ClearQueue =>
            new DelegateCommand(async () =>
            {
                foreach (var item in QueuedVideos)
                {
                    item.IsPanelEnabled = false;
                    await Task.Delay(25);
                }
                await Task.Delay(300);
                QueuedVideos.Clear();
            }, () => QueuedVideos.Count > 0);

        public ICommand ChooseSaveDirectory =>
            new DelegateCommand(() =>
            {
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                if(dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
                {
                    SaveDirectoryPath = dialog.SelectedPath;
                }
            }, () => true);

    }
}
