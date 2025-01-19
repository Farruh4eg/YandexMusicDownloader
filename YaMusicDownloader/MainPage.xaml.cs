using System.Diagnostics;
<<<<<<< HEAD
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
=======
using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
using Yandex.Music.Api.Extensions.API;
using Yandex.Music.Api.Models.Common;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client;
<<<<<<< HEAD
=======
using NLog;
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9

namespace YaMusicDownloader
{
    public partial class MainPage : ContentPage
    {
        YandexMusicClientAsync client;
        private static string token;
        private static readonly HttpClient httpClient = new HttpClient();
<<<<<<< HEAD
        static Dictionary<string, string> headers;
        private static string oauthUrl = "https://oauth.yandex.ru/authorize?client_id=39ce9f16b5e5474cb94ac9a663c92f1e&response_type=token&scope=music%3Acontent&scope=music%3Aread&scope=music%3Awrite&redirect_uri=http://localhost:9999/token";
        private static string SECRET = "kzqU4XhfCaY6B6JTHODeq5";

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static int totalTracksToDownload = 0;
        private static int downloadedTracks = 0;

=======
        private static string oauthUrl = "https://oauth.yandex.ru/authorize?client_id=39ce9f16b5e5474cb94ac9a663c92f1e&response_type=token&scope=music%3Acontent&scope=music%3Aread&scope=music%3Awrite&redirect_uri=http://localhost:9999/token";

        private static Logger logger = LogManager.GetCurrentClassLogger();

>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
        public MainPage()
        {
            InitializeComponent();
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new YProductTypeConverter() },
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            InitDotEnv();
            Server.OnTokenReceived += UpdateTokenEntry;
            InitYaMusic();
        }

        private void InitDotEnv()
        {
            try
            {
                if (File.Exists(".env"))
                {
                    using (StreamReader sr = File.OpenText(".env"))
                    {
                        TokenEntry.Text = sr.ReadLine()?.Split("TOKEN=")[1];
                        DownloadLiked.IsEnabled = true;
                        DownloadCurrent.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error initializing .env file");
            }
        }

        private void GetToken(object sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var server = new Server();
                    _ = server.Start();

                    await UpdateUI(async () =>
                    {
                        await Launcher.OpenAsync(oauthUrl);
                    });
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error in GetToken method");
                    await UpdateUI(() =>
                    {
                        DisplayAlert("Error", ex.Message, "OK");
                    });
                }
            });
        }

        private void UpdateTokenEntry(string token)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TokenEntry.Text = token;
                    DownloadLiked.IsEnabled = true;
                    DownloadCurrent.IsEnabled = true;
                    InitYaMusic();
                });

                if (File.Exists(".env"))
                {
                    var lines = File.ReadAllLines(".env").ToList();
                    bool tokenUpdated = false;

                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith("TOKEN="))
                        {
                            lines[i] = "TOKEN=" + token;
                            tokenUpdated = true;
                            break;
                        }
                    }

                    if (!tokenUpdated)
                    {
                        lines.Add("TOKEN=" + token);
                    }

                    File.WriteAllLines(".env", lines);
                }
                else
                {
                    File.WriteAllText(".env", "TOKEN=" + token);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error updating token entry");
            }
        }

        private async void InitYaMusic()
        {
            token = TokenEntry.Text;
            try
            {
                client = new YandexMusicClientAsync();
                await client.Authorize(token);
<<<<<<< HEAD
                headers = new Dictionary<string, string>
        {
            { "Authorization", $"OAuth {token}" },
            { "X-Yandex-Music-Client", "YandexMusicDesktopAppWindows/5.35.0" }
        };
=======
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error authorizing Yandex Music client");
            }
        }

        private async void DownloadLikedTracks(object sender, EventArgs e)
        {
            DownloadInfo.Text = "";
            DownloadLiked.IsEnabled = false;
            DownloadLiked.Text = "Запрашивается";
<<<<<<< HEAD
            totalTracksToDownload = 0;
            downloadedTracks = 0;
=======
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9

            var result = await FolderPicker.Default.PickAsync();

            await Task.Run(async () =>
            {
                var invalidFileNameChars = Path.GetInvalidFileNameChars();
                if (result.IsSuccessful)
                {
                    try
                    {
                        var tracks = await client.GetLikedTracks();
                        int tracksCount = tracks.Count;

<<<<<<< HEAD
                        var existingFiles = new HashSet<string>(Directory.GetFiles(result.Folder.Path).Select(file => Path.GetFileNameWithoutExtension(file)));
=======
                        var existingFiles = new HashSet<string>(Directory.GetFiles(result.Folder.Path).Select(file => Path.GetFileName(file)));
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
                        int existingFilesCounter = 0;

                        await UpdateUI(() =>
                        {
                            TracksFoundCount.Text = tracksCount.ToString();
                            TracksExistingLayout.IsVisible = true;
<<<<<<< HEAD
                            OverallDownloadProgressBar.IsVisible = true;
                            OverallDownloadProgressBar.Progress = 0;
                        });

                        totalTracksToDownload = tracksCount;

=======
                        });

>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
                        foreach (var track in tracks)
                        {
                            await UpdateUI(() => TracksExistingCount.Text = $"{existingFilesCounter}");
                            var artistsNameList = new List<string>();
                            track.Artists.ForEach(artist => artistsNameList.Add(artist.Name));
                            string artistsJoined = String.Join(", ", artistsNameList);
<<<<<<< HEAD
                            string trackFileName = String.Concat($"{artistsJoined} - {track.Title}".Split(invalidFileNameChars));
=======
                            string trackFileName = String.Concat($"{artistsJoined} - {track.Title}".Split(invalidFileNameChars)) + ".mp3";
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9

                            if (existingFiles.Contains(trackFileName))
                            {
                                ++existingFilesCounter;
                                Debug.WriteLine($"Found: {trackFileName}. Continuing");
                                continue;
                            }

                            try
                            {
                                await DownloadTrack(track, result.Folder.Path);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, $"Error downloading track '{track.Title}'");
                            }
<<<<<<< HEAD

                            downloadedTracks++;
                            await UpdateUI(() => OverallDownloadProgressBar.Progress = (double)downloadedTracks / totalTracksToDownload);
=======
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
                        }

                        await UpdateUI(() => DownloadInfo.Text += "Скачивание ваших треков завершено.\n");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error fetching liked tracks");
                    }
                }
            });

            DownloadLiked.IsEnabled = true;
            DownloadLiked.Text = "Скачать любимые треки";
        }

        private async Task UpdateUI(Action action)
        {
            await MainThread.InvokeOnMainThreadAsync(action);
        }

        private async void DownloadCurrentTrack(object sender, EventArgs e)
        {
            try
            {
                await UpdateUI(() =>
                {
                    DownloadCurrent.IsEnabled = false;
                    DownloadCurrent.Text = "Запрашивается";
                });

                Uri apiEndpoint = new Uri($"https://api.mipoh.ru/get_current_track_beta?ya_token={token}");
                var response = await httpClient.GetStringAsync(apiEndpoint);
                var jsonResponse = JsonConvert.DeserializeObject<JObject>(response);

                string trackId = jsonResponse?["track"]?["track_id"].ToString();
                if (string.IsNullOrEmpty(trackId))
                {
                    logger.Warn("Failed to retrieve the current track ID.");
                    await DisplayAlert("Error", "Failed to retrieve the current track ID.", "OK");
                    return;
                }

                YTrack track = await client.GetTrack(trackId);

                var result = await FolderPicker.Default.PickAsync();
                if (result.IsSuccessful)
                {
                    await DownloadTrack(track, result.Folder.Path);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in DownloadCurrentTrack method");
                await DisplayAlert("Ошибка", $"Ошибка определения текущего трека. Попробуйте включить трек в веб-версии Яндекс Музыки, а не в приложении.\n{ex.Message}", "OK");
            }
            finally
            {
                await UpdateUI(() =>
                {
                    DownloadCurrent.IsEnabled = true;
                    DownloadCurrent.Text = "Скачать текущий трек";
                });
            }
        }

<<<<<<< HEAD
        private async void RecognizeTrack(object sender, EventArgs e)
        {
            IStatusMessage statusMessage = new StatusMessage();

            AudioRecorder recorder = new AudioRecorder();

            try
            {
                statusMessage.EditText("Recording audio for 8 seconds...");

                byte[] audioData = await recorder.RecordAudioAsync(8);
                await AudioConverter.ConvertToOggWithOpusAsync(audioData, "sample.ogg");
                byte[] audioOgg = await File.ReadAllBytesAsync("sample.ogg");

                Recognition recognition = new Recognition(audioOgg, statusMessage);


                string trackId = await recognition.GetTrackIdAsync();

                //File.Delete("sample.ogg");

                if (trackId != null)
                {
                    statusMessage.EditText($"Track ID found: {trackId}");

                    var track = await client.GetTrack(trackId);
                    if (track != null)
                    {
                        statusMessage.EditText($"Track: {track.Artists[0].Name} - {track.Title}");
                    }
                }
                else
                {
                    statusMessage.EditText("No track matched.");
                }
            }
            catch (Exception ex)
            {
                statusMessage.EditText($"Error: {ex.Message}");
            }
        }


        private async Task SaveTrackToFile(YTrack track, string path, string downloadUrl, string codec)
        {
            try
            {
                var artistsNameList = new List<string>();
                track.Artists.ForEach(artist => artistsNameList.Add(artist.Name));
                string artistsJoined = String.Join(", ", artistsNameList);
                string trackFileName = String.Concat($"{artistsJoined} - {track.Title}".Split(Path.GetInvalidFileNameChars())) + (codec == "flac" ? ".flac" : ".mp3");

                string filePath = Path.Combine(path, trackFileName);

                await UpdateUI(() => DownloadInfo.Text += $"({codec}) Скачивается: {artistsJoined} - {track.Title}\n");
                var trackData = await httpClient.GetByteArrayAsync(downloadUrl);


                await File.WriteAllBytesAsync(filePath, trackData);
                await UpdateUI(() =>
                {
                    DownloadProgressInfo.Text = $"Скачано: {artistsJoined} - {track.Title}";
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error saving track '{track.Title}' to file");
            }
        }

        private async Task DownloadTrack(YTrack track, string path)
        {
            if (DownloadFlac.IsChecked)
            {
                await DownloadTrackFlac(track, path);
                return;
            }

            try
            {
                Uri trackLink = new Uri(await track.GetLinkAsync());

                await SaveTrackToFile(track, path, trackLink.ToString(), "mp3");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error downloading track '{track.Title}'");
            }
        }

        private async Task DownloadTrackFlac(YTrack track, string path)
        {
            string TRACK_ID = track.Id;
            long TIMESTAMP = DateTimeOffset.Now.ToUnixTimeSeconds();

            string data = $"{TIMESTAMP}{TRACK_ID}losslessflacaache-aacmp3raw";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SECRET)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                string sign = Convert.ToBase64String(hash).TrimEnd('=');

                var queryParams = new Dictionary<string, string>
        {
            { "ts", TIMESTAMP.ToString() },
            { "trackId", TRACK_ID },
            { "quality", "lossless" },
            { "codecs", "flac,aac,he-aac,mp3" },
            { "transports", "raw" },
            { "sign", sign }
        };

                var baseUrl = "https://api.music.yandex.net/get-file-info";
                var url = $"{baseUrl}?{string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))}";

                foreach (var header in headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                try
                {
                    var response = await httpClient.GetStringAsync(url);
                    var jsonResponse = JsonConvert.DeserializeObject<JObject>(response);

                    string flacUrl = jsonResponse?["result"]?["downloadInfo"]?["url"]?.ToString();
                    string codec = jsonResponse?["result"]?["downloadInfo"]?["codec"]?.ToString();

                    if (!string.IsNullOrEmpty(flacUrl))
                    {
                        await SaveTrackToFile(track, path, flacUrl, codec);
                    }
                    else
                    {
                        logger.Warn($"FLAC download URL is missing for track '{track.Title}'");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error downloading FLAC for track '{track.Title}'");
                }
                finally
                {
                    httpClient.DefaultRequestHeaders.Clear();
                }
=======
        private async Task DownloadTrack(YTrack track, string path)
        {
            try
            {
                Uri trackLink = new Uri(await track.GetLinkAsync());
                var artistsNameList = new List<string>();
                track.Artists.ForEach(artist => artistsNameList.Add(artist.Name));
                string artistsJoined = String.Join(", ", artistsNameList);
                string trackFileName = String.Concat($"{artistsJoined} - {track.Title}".Split(Path.GetInvalidFileNameChars())) + ".mp3";

                string filePath = Path.Combine(path, trackFileName);

                var trackData = await httpClient.GetByteArrayAsync(trackLink);
                await UpdateUI(() => DownloadInfo.Text += $"Скачивается: {artistsJoined} - {track.Title}\n");

                await File.WriteAllBytesAsync(filePath, trackData);
                DownloadProgressInfo.Text = $"Скачано: {artistsJoined} - {track.Title}";
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error downloading track");
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
            }
        }

        private async void SearchTrack(object sender, EventArgs e)
        {
            string searchText = SearchTitle.Text;
            if (!String.IsNullOrEmpty(searchText))
            {
                SearchResultsContainer.Children.Clear();
                try
                {
                    var searchResults = await client.Search(searchText, YSearchType.Track, 0, 10);
                    TracksFoundCount.Text = searchResults.Tracks.Results.Count.ToString();
                    foreach (var searchResult in searchResults.Tracks.Results)
                    {
                        string trackLink = await searchResult.GetLinkAsync();
                        string trackTitle = searchResult.Title;
                        var artistsNameList = new List<string>();
                        searchResult.Artists.ForEach(artist => artistsNameList.Add(artist.Name));
                        string artistsJoined = String.Join(", ", artistsNameList);

                        DisplaySearchResults(searchResult);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error searching track");
                }
            }
        }

        private void DisplaySearchResults(YTrack track)
        {
            try
            {
                string trackTitle = track.Title;
                var artistsNameList = new List<string>();
                track.Artists.ForEach(artist => artistsNameList.Add(artist.Name));
                string artistsJoined = String.Join(", ", artistsNameList);

                var stackLayout = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    VerticalOptions = LayoutOptions.Center,
                    Spacing = 10
                };

                var trackLabel = new Label { Text = $"{artistsJoined} - {trackTitle}" };

                var downloadBtn = new ImageButton
                {
                    WidthRequest = 32,
                    HeightRequest = 32,
                    Scale = 0.5,
                    Source = "download.png",
                    BackgroundColor = Colors.White
                };

                downloadBtn.Clicked += async (obj, e) =>
                {
                    var result = await FolderPicker.Default.PickAsync();
                    if (result.IsSuccessful)
                    {
                        await DownloadTrack(track, result.Folder.Path);
                    }
                };

                stackLayout.Children.Add(trackLabel);
                stackLayout.Children.Add(downloadBtn);

                SearchResultsContainer.Children.Add(stackLayout);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error displaying search results");
            }
        }

<<<<<<< HEAD
        private void FlacCheckClicked(object sender, EventArgs e)
        {
            DownloadFlac.IsChecked = !DownloadFlac.IsChecked;
        }

=======
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
        public class YProductTypeConverter : JsonConverter<YProductType>
        {
            public override YProductType ReadJson(JsonReader reader, Type objectType, YProductType existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                string value = reader.Value?.ToString();
                if (value == "dummy")
                {
                    return YProductType.Subscription;
                }

                return (YProductType)Enum.Parse(typeof(YProductType), value);
            }

            public override void WriteJson(JsonWriter writer, YProductType value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }
        }
<<<<<<< HEAD

        public class StatusMessage : IStatusMessage
        {
            public void EditText(string message)
            {
                Debug.WriteLine(message);
            }
        }
=======
>>>>>>> 37d60fc36da109df882a848f23ef29274aae6ee9
    }
}
