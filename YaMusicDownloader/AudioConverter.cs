using System.Diagnostics;

namespace YaMusicDownloader
{
    class AudioConverter
    {
        public static async Task ConvertToOggWithOpusAsync(byte[] pcmData, string outputFilePath)
        {
            string tempPcmFile = Path.Combine(Path.GetTempPath(), "temp_audio.pcm");
            await File.WriteAllBytesAsync(tempPcmFile, pcmData);

            string ffmpegCommand = $"-f s16le -ar 16000 -ac 1 -i \"{tempPcmFile}\" -c:a libopus \"{outputFilePath}\"";

            Debug.WriteLine("BEFORE FFMPEG");
            await RunFFmpegCommandAsync(ffmpegCommand);
            Debug.WriteLine("AFTER FFMPEG");
        }

        private static async Task RunFFmpegCommandAsync(string command)
        {
            string ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "tools", "ffmpeg.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                Debug.WriteLine("PRCESS FFMPEG");

                if (process == null)
                {
                    Console.WriteLine("Failed to start FFmpeg process.");
                    return;
                }

                await process.WaitForExitAsync();

                using (var reader = process.StandardError)
                {
                    string stderr = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        Console.WriteLine($"FFmpeg error: {stderr}");
                    }
                }
            }
        }
    }
}
