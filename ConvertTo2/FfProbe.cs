using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Size = System.Windows.Size;

namespace ConvertTo2
{
    internal enum MediaTypes
    {
        None,
        Video,
        Audio,
        Image
    }

    /*internal static class FfProbe
    {
        private List<Dictionary<string, string>> ParseData(string consoleOutput)
        {
            var data = new List<Dictionary<string, string>>();
            bool ignore = false;

            foreach (var line in consoleOutput.Replace("\r", "").Split('\n'))
            {
                if (line == "[STREAM]")
                {
                    data.Add(new Dictionary<string, string>());
                }
                else if (line == "[/STREAM]")
                {
                    continue;
                }
                else if (line == "[FORMAT]")
                {
                    ignore = true;
                }
                else if (line == "[/FORMAT]")
                {
                    ignore = false;
                }
                else if (ignore)
                {
                    continue;
                }
                else
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        data[^1].Add(parts[0], parts[1]);
                    }
                }
            }

            return data;
        }

        internal static MediaTypes LoadData()
        { }
    }*/

    internal static class FfProbe
    {
        internal static MediaTypes LoadData()
        {
            if (string.IsNullOrEmpty(FfmpegData.InputFile))
            {
                MessageBox.Show("Kein Eingabedateipfad gefunden.");
                return MediaTypes.None;
            }

            // ffprobe Befehl ausführen
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v quiet -show_format -show_streams \"{FfmpegData.InputFile}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(processStartInfo))
                using (var reader = process.StandardOutput)
                {
                    string output = reader.ReadToEnd();
                    var data = ParseData(output);
                    return ProcessParsedData(data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Ausführung von ffprobe: {ex.Message}");
            }

            return MediaTypes.None;
        }

        private static List<Dictionary<string, string>> ParseData(string consoleOutput)
        {
            var data = new List<Dictionary<string, string>>();
            bool ignore = false;

            foreach (var line in consoleOutput.Replace("\r", "").Split('\n'))
            {
                if (line == "[STREAM]")
                {
                    data.Add(new Dictionary<string, string>());
                }
                else if (line == "[/STREAM]")
                {
                    continue;
                }
                else if (line == "[FORMAT]")
                {
                    ignore = true;
                }
                else if (line == "[/FORMAT]")
                {
                    ignore = false;
                }
                else if (ignore)
                {
                    continue;
                }
                else if (data.Count > 0)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        data[^1][parts[0]] = parts[1];
                    }
                }
            }

            return data;
        }

        private static MediaTypes ProcessParsedData(List<Dictionary<string, string>> data)
        {
            if (data.Count == 0)
            {
                MessageBox.Show("No data received from ffprobe.");
                return MediaTypes.None;
            }

            // Check if it's a video, audio, or image based on available fields
            bool hasVideo = data.Any(d => d.ContainsKey("codec_type") && d["codec_type"] == "video" && d["duration"] != "N/A" && d["bit_rate"] != "N/A");
            bool hasAudio = data.Any(d => d.ContainsKey("codec_type") && d["codec_type"] == "audio");
            bool isImage = data.Any(d => d.ContainsKey("codec_type") && d["codec_type"] == "video" && d["duration"] == "N/A" && d["bit_rate"] == "N/A");
            bool isGif = data.Any(d => d.ContainsKey("codec_name") && d["codec_name"].ToLower() == "gif");

            // Process video data (including GIF)
            if (hasVideo || isGif)
            {
                var videoStream = data.First(d => d["codec_type"] == "video" && (d["duration"] != "N/A" || isGif));
                FfmpegData.VideoCodec = videoStream["codec_name"];
                FfmpegData.Resolution = new Size(
                    int.Parse(videoStream["width"]),
                    int.Parse(videoStream["height"])
                );
                FfmpegData.Fps = CalculateFps(videoStream["avg_frame_rate"]);

                if (videoStream.ContainsKey("bit_rate") && videoStream["bit_rate"] != "N/A")
                {
                    FfmpegData.VideoBitrate = double.Parse(videoStream["bit_rate"]) / 1000.0; // kbps
                }
            }

            // Process audio data
            if (hasAudio)
            {
                var audioStream = data.First(d => d["codec_type"] == "audio");
                FfmpegData.AudioCodec = audioStream["codec_name"];

                if (audioStream.ContainsKey("bit_rate") && audioStream["bit_rate"] != "N/A")
                {
                    FfmpegData.AudioBitrate = double.Parse(audioStream["bit_rate"]) / 1000.0; // kbps
                }

                if (audioStream.ContainsKey("sample_rate") && audioStream["sample_rate"] != "N/A")
                {
                    FfmpegData.AudioSampleRate = int.Parse(audioStream["sample_rate"]);
                }

                if (audioStream.ContainsKey("channels") && audioStream["channels"] != "N/A")
                {
                    FfmpegData.AudioChannels = (AudioChannels)int.Parse(audioStream["channels"]);
                }
            }

            // Process image data (if it's an image)
            if (isImage)
            {
                var imageStream = data.First(d => d["codec_type"] == "video" && d["duration"] == "N/A");
                FfmpegData.Resolution = new Size(
                    int.Parse(imageStream["width"]),
                    int.Parse(imageStream["height"])
                );
                // No duration, bitrate, or FPS for images
            }

            // Set duration if available (for video and audio)
            var formatData = data.FirstOrDefault(d => d.ContainsKey("duration"));
            if (formatData != null && formatData["duration"] != "N/A")
            {
                double durationInSeconds = double.Parse(formatData["duration"], CultureInfo.InvariantCulture);
                FfmpegData.Duration = durationInSeconds;

                // CutStart remains 0, CutEnd is set to the total duration
                FfmpegData.CutStart = TimeSpan.Zero;
                FfmpegData.CutEnd = TimeSpan.FromSeconds(durationInSeconds);
            }

            // Determine media type
            if (hasVideo || isGif)
            {
                return hasAudio ? MediaTypes.Video : MediaTypes.Video; // Treat GIF as video
            }
            else if (hasAudio)
            {
                return MediaTypes.Audio;
            }
            else if (isImage)
            {
                return MediaTypes.Image;
            }

            return MediaTypes.None; // If no valid media type was found
        }

        private static double CalculateFps(string avgFrameRate)
        {
            var parts = avgFrameRate.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out int numerator) && int.TryParse(parts[1], out int denominator))
            {
                return (double)numerator / denominator;
            }
            return 0;
        }
    }
}