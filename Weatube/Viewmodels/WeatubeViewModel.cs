using DevExpress.Mvvm;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Weatube.Models;
using Weatube.Properties;

namespace Weatube.Viewmodels
{
    class WeatubeViewModel : NotifyPropertyChangedBehavior
    {
        public ObservableCollection<VideoModel> QueuedVideos { get; set; }

        public SuggestionModel suggestion { get; set; }

        private string _SearchVideo { get; set; }

        public string _SaveDirectoryPath { get; set; }
        public string SaveDirectoryPath 
        { 
            get
            {
                return _SaveDirectoryPath;
            } 
            set
            {
                _SaveDirectoryPath = value;
                Settings.Default.DefaultSavePath = value;
            }
        }

        public string SearchVideo
        {
            get => _SearchVideo;
            set
            {
                _SearchVideo = value;
                _ = Task.Run(async () =>
                  {
                      if (_SearchVideo.Length < 1)
                      {
                          suggestion.Disable();
                          await Task.Delay(300);
                          suggestion = new SuggestionModel();
                          return;
                      }
                      async Task<bool> UserKeepsTyping()
                      {
                          string txt = _SearchVideo;
                          await Task.Delay(500);
                          return txt != _SearchVideo;
                      }
                      if (await UserKeepsTyping()) return;

                      var vid = new YoutubeDL(_SearchVideo);
                      if (await vid.InitAsync() != null)
                          suggestion = new SuggestionModel(vid);
                  });
            }
        }

        public WeatubeViewModel()
        {
            suggestion = new SuggestionModel();
            QueuedVideos = new ObservableCollection<VideoModel>();
            _SaveDirectoryPath = (Settings.Default.DefaultSavePath.Length > 0) 
                ? Settings.Default.DefaultSavePath
                : Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), "Downloads");
        }

        public ICommand AddVideo => 
            new DelegateCommand<List<YoutubeDL.Video>>(async (videos) =>
                {
                    foreach (var video in videos)
                    {
                        video.SelectedFormat = suggestion.SelectedType;
                        QueuedVideos.Add(new VideoModel(video));
                        await Task.Delay(5);
                        QueuedVideos.Last().IsPanelEnabled = true;
                    }
                    SearchVideo = "";
                }, (videos) => suggestion.SuggestedVideoName != null && SearchVideo.Length > 1);

        public ICommand DeleteVideo =>
            new DelegateCommand<VideoModel>(async (video) =>
            {
                QueuedVideos[QueuedVideos.IndexOf(video)].IsPanelEnabled = false;
                await Task.Delay(300);
                QueuedVideos.Remove(video);
                CommandManager.InvalidateRequerySuggested();
            }, (video) => QueuedVideos.Contains(video));

        public ICommand DownloadVideos =>
            new DelegateCommand(async () =>
            {
                for (int i = 0; i < QueuedVideos.Count; i++)
                {
                    var filename = SaveDirectoryPath + '\\' + QueuedVideos[i].YoutubeVideo.Name +
                        (QueuedVideos[i].YoutubeVideo.SelectedFormat.Name == "mp3" ? ".mp3" : ".mp4");
                    var process = YoutubeDL.RunProcess(QueuedVideos[i].YoutubeVideo.GetCommandArguments(filename));
                    while (!process.HasExited)
                        QueuedVideos[i].DownloadState =
                        Utils.DownloadStateChange(Utils.PercentFromOutput(await process.StandardOutput.ReadLineAsync()));
                    if(!File.Exists(filename))
                        QueuedVideos[i].DownloadState = 
                            Utils.DownloadStateChange(Utils.PercentFromOutput(null, await process.StandardError.ReadLineAsync()));
                }
                CommandManager.InvalidateRequerySuggested();
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
                CommandManager.InvalidateRequerySuggested();
            }, () => QueuedVideos.Count > 0);

        public ICommand ChooseSaveDirectory =>
            new DelegateCommand(() =>
            {
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                if(dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
                    SaveDirectoryPath = dialog.SelectedPath;
            }, () => true);

    }
}
