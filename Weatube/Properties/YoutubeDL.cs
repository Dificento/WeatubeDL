using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using WebPWrapper;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Weatube {
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class RawYTDLVideo {
		public class RawThumbnail {
			public int height;
			public int width;
			public string id;
			public string resolution;
			public string url;
		}

		public class RawFormat {
			public string format_id;
			public int? height;
			public int? width;

			// Not present for some extractors
			public string container;
			public string format_note;
			public string ext;
			public float? fps;
			public long? filesize;
		}

		public RawThumbnail[] thumbnails;

		public string channel;
		public string fulltitle;
		public string uploader;

		public string description;
		public float duration;

		public string id;
		public string webpage_url; // URL of webpage for video
		public string extractor_key; // Nice extractor name
		public string ext; // Default ext with format download

		public RawFormat[] formats; // List of all available formats to download

		public string playlist; // Playlist name if present
	}

	internal class ThumbnailComparer : IComparer<RawYTDLVideo.RawThumbnail> {
		public int Compare(RawYTDLVideo.RawThumbnail x, RawYTDLVideo.RawThumbnail y) {
			if (ReferenceEquals(x, y)) return 0;
			if (ReferenceEquals(null, y)) return 1;
			if (ReferenceEquals(null, x)) return -1;
			return x.height.CompareTo(y.height);
		}
	}

	/// <summary>
	/// Класс обработки новой ссылки через YoutubeDL и получения <see cref="Video"/>, адресуемых по ней
	/// </summary>
	public class YoutubeDL {
		private static string DefaultPlaylistName = "Playlist";
		private static Bitmap DefaultPlaylistThumbnail = null;

		public YoutubeDLRequestType Type { get; private set; }
		public Bitmap Thumbnail { get; private set; }

		public ImageSource ImageSourceFromBitmap() {
			MemoryStream ms = new MemoryStream();
			Thumbnail.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
			BitmapImage image = new BitmapImage();
			image.BeginInit();
			ms.Seek(0, SeekOrigin.Begin);
			image.StreamSource = ms;
			image.EndInit();
			image.Freeze();
			return image;
		}

		public string Name { get; private set; }

		/// <summary>
		/// Ссылка, которая была подана на обработку
		/// </summary>
		public string SourceUrl { get; }

		/// <summary>
		/// Инициализован ли текущий объект YoutubeDL (были ли загружены видео по полученной ссылке <see cref="SourceUrl"/>)
		/// </summary>
		public bool Initialized { get; private set; }

		private List<RawYTDLVideo> _rawYtdlVideos;
		public IReadOnlyList<RawYTDLVideo> RawOutput => _rawYtdlVideos.AsReadOnly();

		private List<Video> _videoList;

		/// <summary>
		/// Список видео, экспортированных после инициализации (<see cref="Initialized"/>)
		/// </summary>
		public IReadOnlyList<Video> VideoList => _videoList.AsReadOnly();

		/// <summary>
		/// Создать НЕИНИЦИАЛИЗИРОВАННЫЙ экземпляр класса.
		/// Для инициализации (загрузки видео по ссылке) использовать <see cref="Init"/> и <see cref="InitAsync"/>
		/// </summary>
		/// <param name="sourceUrl"></param>
		public YoutubeDL(string sourceUrl) {
			SourceUrl = sourceUrl;
			Initialized = false;
			_videoList = new List<Video>();
		}

		/// <summary>
		/// Создает и асинхронно инициализирует и возвращает новый объект <see cref="YoutubeDL"/>
		/// </summary>
		public static async Task<YoutubeDL> ConstructAndInit(string sourceUrl) {
			var youtubeDL = new YoutubeDL(sourceUrl);
			await youtubeDL.InitAsync();
			return youtubeDL;
		}

		/// <summary>
		/// Асинхронная версия <see cref="Init"/>
		/// </summary>
		/// <returns></returns>
		public async Task<IEnumerable<Video>> InitAsync() {
			var output = await Task.Run(Init);
			return output;
		}

		public static Process RunProcess(string arguments) {
			var startInfo = new ProcessStartInfo {
				FileName = "yt-dlp.exe",
				Arguments = arguments,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			// Создаем процесс
			var process = Process.Start(startInfo);
			return process;
		}

		/// <summary>
		/// Открывает процесс YoutubeDL и перехватывает вывод в <see cref="RawOutput"/>
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Video> Init() {
			// Создаем процесс
			var process = RunProcess("--simulate --print-json --no-check-certificate " + "\"" + SourceUrl + "\"");

			// Создаем ОБЪЕКТ
			string output;
			_rawYtdlVideos = new List<RawYTDLVideo>();
			while ((output = process.StandardOutput.ReadLine()) != null) {
				var m = JsonConvert.DeserializeObject<RawYTDLVideo>(output);
				if (m == null) continue;
				_rawYtdlVideos.Add(m);
			}

			// Создаем ПРЕЗЕНТОР
			_videoList = new List<Video>();
			var taskList = new List<Task>();

			foreach (var rawVideo in _rawYtdlVideos) {
				var availableFormats = new HashSet<Video.NormalOutputFormat>();
				// Parse formats
				foreach (var rawFormat in rawVideo.formats) {
					if (rawFormat.width == null || rawFormat.height == null) continue;
					var format = new Video.NormalOutputFormat(rawFormat.width.Value, rawFormat.height.Value);
					availableFormats.Add(format);
				}

				var sortableFormats = availableFormats.ToList();
				sortableFormats.Sort();

				// take the PRE last preview (not the worst, but still much smaller than the max one)
				Array.Sort(rawVideo.thumbnails, new ThumbnailComparer());
				var thumbnailUrl = (rawVideo.thumbnails.Length > 1
					? rawVideo.thumbnails[rawVideo.thumbnails.Length - 2]
					: rawVideo.thumbnails[0]).url;

				// todo: too bad... figure something with this "async longing" shit

				Bitmap thumbnail = null;
				var request = WebRequest.Create(thumbnailUrl);
				var response = request.GetResponse();
				var stream = response.GetResponseStream();
				MemoryStream seekableMemStream = new MemoryStream();
				stream.CopyTo(seekableMemStream);
				switch (response.ContentType) {
					case "image/webp":
						var rawWebP = seekableMemStream.ToArray();
						using (var webp = new WebP())
							thumbnail = webp.Decode(rawWebP);
						break;
					case "image/bmp":
					case "image/gif":
					case "image/jpeg":
					case "image/png":
					case "image/tiff":
						thumbnail = new Bitmap(seekableMemStream);
						break;
					default:
						throw new Exception($"Unsupported format {response.ContentType}");
				}

				var video = new Video(
					rawVideo.fulltitle,
					rawVideo.extractor_key,
					rawVideo.webpage_url,
					rawVideo.uploader,
					rawVideo.ext == "mp3",
					sortableFormats,
					thumbnail
				);
				_videoList.Add(video);
			}


			// fill main object
			if (_videoList.Count > 1) {
				Type = YoutubeDLRequestType.YOUTUBEDL_REQUEST_TYPE_PLAYLIST;
				Name = _rawYtdlVideos[0].playlist ?? DefaultPlaylistName;
				Thumbnail = _videoList[0].Thumbnail ?? DefaultPlaylistThumbnail;
			}
			else if (_videoList.Count == 1) {
				Type = YoutubeDLRequestType.YOUTUBEDL_REQUEST_TYPE_SINGLE;
				Name = _videoList[0].Name;
				Thumbnail = _videoList[0].Thumbnail;
			}
			else return null;

			process.WaitForExit();
			process.Dispose();
			Initialized = true;
			return VideoList;
		}

		/* -- */

		public enum YoutubeDLRequestType {
			YOUTUBEDL_REQUEST_TYPE_PLAYLIST,
			YOUTUBEDL_REQUEST_TYPE_SINGLE,
		}

		public class Video {
			/// <summary>
			/// Список форматов экспорта, доступных каждому видео
			/// </summary>
			private static readonly OutputFormat[] DefaultFormats =
				{new OutputFormat("best", OutputFormat.FormatType.FormatTypeNormal), new AudioOutputFormat("mp3")};

			/// <summary>
			/// Тип сервиса, с которого получено видео (Youtube, Vimeo...)
			/// </summary>
			public string Type { get; }

			/// <summary>
			/// Ссылка на видео в указанном сервисе <see cref="Type"/>
			/// </summary>
			public string Source { get; }

			/// <summary>
			/// Имя видео
			/// </summary>
			public string Name { get; }

			/// <summary>
			/// Имя загрузившего
			/// </summary>
			public string Uploader { get; }

			/// <summary>
			/// Видео? Нет - аудио
			/// </summary>
			public bool IsVideo { get; }

			private readonly List<OutputFormat> _availableFormats;
			public IList<OutputFormat> AvailableFormats => _availableFormats.AsReadOnly();
			public OutputFormat SelectedFormat;

			/// <summary>
			/// Превью видео
			/// </summary>
			public Bitmap Thumbnail { get; }

			public ImageSource ImageSourceFromBitmap() {
				MemoryStream ms = new MemoryStream();
				Thumbnail.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				ms.Seek(0, SeekOrigin.Begin);
				image.StreamSource = ms;
				image.EndInit();
				image.Freeze();
				return image;
			}

			public Video(string name, string type, string source, string uploader, bool isVideo,
				IEnumerable<OutputFormat> availableFormats,
				Bitmap thumbnail) {
				Name = name;
				Type = type;
				Uploader = uploader;
				Source = source;
				IsVideo = isVideo;
				_availableFormats = new List<OutputFormat>(DefaultFormats);
				_availableFormats.AddRange(availableFormats);
				SelectedFormat = _availableFormats[0];
				Thumbnail = thumbnail;
			}

			/// <summary>
			/// Получить список аргументов для экспорта видео через yt-dlp
			/// </summary>
			/// <param name="filename">имя файла для сохранения видео</param>
			public string GetCommandArguments(string filename) {
				var args = 
					GetCommandArguments() + 
					(!IsVideo ? " --embed-thumbnail " : "") + // не видео - докачиваем превью
					" --add-metadata --xattrs --no-check-certificate -o \"" + filename + "\"";
				return args;
			}


			/// <summary>
			/// Получить список аргументов для экспорта видео через yt-dlp
			/// </summary>
			public string GetCommandArguments() {
				switch (SelectedFormat.Type) {
					case OutputFormat.FormatType.FormatTypeNormal:
						return "-f \"" + SelectedFormat.GetCommand() + "\"" + " " + Source;
					case OutputFormat.FormatType.FormatTypeAudioOnly:
						return "--extract-audio --audio-format \"" + SelectedFormat.GetCommand() + "\"" + " " + Source;
				}

				return null;
			}

			/* -- */

			/// <summary>
			/// Класс для определения формата экспорта найденных видео
			/// </summary>
			public class OutputFormat : IComparable<OutputFormat> {
				public enum FormatType {
					/// <summary>
					/// Тип формата, который определяет качество аудио и видео одновременно (например, <b>best</b>)
					/// </summary>
					FormatTypeNormal,

					/// <summary>
					/// Тип формата, который определяет только качество аудио (для <b>--extract-audio</b>)
					/// </summary>
					FormatTypeAudioOnly
				}

				public string Name { get; }
				public FormatType Type;

				/// <summary>
				/// Создание уникального формата с именем
				/// </summary>
				public OutputFormat(string name, FormatType type) {
					Name = name.ToUpper();
					Type = type;
				}

				public virtual string GetCommand() {
					return Name.ToLower();
				}

				/* -- */

				public override bool Equals(object obj) {
					//Check for null and compare run-time types.
					if (obj == null || GetType() != obj.GetType()) return false;
					var p = (OutputFormat) obj;
					return Name.Equals(p.Name);
				}

				public override int GetHashCode() {
					return Name.GetHashCode();
				}

				public virtual int CompareTo(OutputFormat outputFormat) {
					return string.Compare(Name, outputFormat.Name, StringComparison.Ordinal);
				}

				public override string ToString() {
					return Name;
				}
			}

			/// <summary>
			/// Класс для определения формата экспорта только звуковой дорожки
			/// </summary>
			public class AudioOutputFormat : OutputFormat {
				/// <summary>
				/// Создание формата по расширению выходного аудио
				/// </summary>
				public AudioOutputFormat(string extension) : base(extension, FormatType.FormatTypeAudioOnly) { }
			}

			/// <summary>
			/// Класс для определеиня формата экспорта видео+аудио с выбором разрешения кадра
			/// </summary>
			public class NormalOutputFormat : OutputFormat {
				public int Width { get; }
				public int Height { get; }

				/// <summary>
				/// Создание формата по паре параметров: ширина и высота
				/// </summary>
				public NormalOutputFormat(int width, int height) : base($"{width}x{height}",
					FormatType.FormatTypeNormal) {
					Width = width;
					Height = height;
				}

				/// <summary>
				/// Достать "функциональную" строку из формата (для использования в качестве аргумента -f для yt-dlp)
				/// Принимает значение имени, если width/height пустые
				/// </summary>
				public override string GetCommand() {
					return (Height == -1 || Width == -1)
						? base.GetCommand()
						: $"best[width<={Width}][height<={Height}]";
				}

				public override int CompareTo(OutputFormat outputFormat) {
					// Форматы видео будут сортироваться по убыванию высоты видео
					return typeof(NormalOutputFormat) != outputFormat.GetType()
						? base.CompareTo(outputFormat)
						: ((NormalOutputFormat) outputFormat).Height.CompareTo(Height);
				}
			}
		}
	}
}