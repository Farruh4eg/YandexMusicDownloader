using System.Diagnostics;
using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yandex.Music.Api.Extensions.API;
using Yandex.Music.Api.Models.Common;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client;
using NLog;

namespace YaMusicDownloader
{
    public partial class MainPage : ContentPage
    {
        YandexMusicClientAsync client;
        private static string token;
        private static readonly HttpClient httpClient = new HttpClient();
        private static string oauthUrl = "https://oauth.yandex.ru/authorize?client_id=39ce9f16b5e5474cb94ac9a663c92f1e&response_type=token&scope=music%3Acontent&scope=music%3Aread&scope=music%3Awrite&redirect_uri=http://localhost:9999/token";

        private static Logger logger = LogManager.GetCurrentClassLogger();

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

                        var existingFiles = new HashSet<string>(Directory.GetFiles(result.Folder.Path).Select(file => Path.GetFileName(file)));
                        int existingFilesCounter = 0;

                        await UpdateUI(() =>
                        {
                            TracksFoundCount.Text = tracksCount.ToString();
                            TracksExistingLayout.IsVisible = true;
                        });

                        foreach (var track in tracks)
                        {
                            await UpdateUI(() => TracksExistingCount.Text = $"{existingFilesCounter}");
                            var artistsNameList = new List<string>();
                            track.Artists.ForEach(artist => artistsNameList.Add(artist.Name));
                            string artistsJoined = String.Join(", ", artistsNameList);
                            string trackFileName = String.Concat($"{artistsJoined} - {track.Title}".Split(invalidFileNameChars)) + ".mp3";

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
    }
}
