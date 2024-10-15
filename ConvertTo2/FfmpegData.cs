using System.IO;
using System.Threading;
using System.Windows;

namespace ConvertTo2
{
    internal static class FfmpegData
    {
        private static int _threads = 0;
        internal static string InputFile { get; set; } = string.Empty; // Invisible
        internal static string OutputFile { get; set; } = string.Empty; // Settings Page
        internal static Size Resolution { get; set; } = new Size(0, 0); // Picture / Video Page
        internal static double Fps { get; set; } = 0; // Video Page
        internal static double Duration { get; set; } = 0; // Video / Audio Page
        internal static double VideoBitrate { get; set; } = 0; // Video Page
        internal static string VideoCodec { get; set; } = string.Empty; // Video Page
        internal static double AudioBitrate { get; set; } = 0; // Audio Page
        internal static string AudioCodec { get; set; } = string.Empty; // Audio Page
        internal static int AudioSampleRate { get; set; } = 44100; // Audio Page
        internal static AudioChannels AudioChannels { get; set; } = AudioChannels.Stereo; // Audio Page
        internal static TimeSpan CutStart { get; set; } = new TimeSpan(0); // Video / Audio Page
        internal static TimeSpan CutEnd { get; set; } = new TimeSpan(0); // Video / Audio Page
        internal static FfmpegPreset Preset { get; set; } = FfmpegPreset.Medium; // Video Page
        internal static int Threads // Settings Page
        {
            get
            {
                int logicalCores = Environment.ProcessorCount;
                return _threads > 0 && _threads <= logicalCores ? _threads : logicalCores;
            }
            set
            {
                _threads = value;
            }
        }

        internal static bool CreateArgs(out string args)
        {
            args = string.Empty;

            // First check if the data is valid
            if (!ValidateData())
            {
                return false;
            }

            // Create the arguments
            var argsBuilder = new System.Text.StringBuilder();

            // Input and output file
            argsBuilder.AppendFormat("-i \"{0}\" ", InputFile);

            // Cut
            if (CutStart != TimeSpan.Zero)
            {
                argsBuilder.AppendFormat("-ss {0} ", CutStart.ToString(@"hh\:mm\:ss\.fff"));
            }
            if (CutEnd != TimeSpan.Zero)
            {
                argsBuilder.AppendFormat("-to {0} ", CutEnd.ToString(@"hh\:mm\:ss\.fff"));
            }

            // Video options
            if (!string.IsNullOrWhiteSpace(VideoCodec))
            {
                argsBuilder.AppendFormat("-c:v {0} ", VideoCodec);
            }
            if (Resolution.Width > 0 && Resolution.Height > 0)
            {
                argsBuilder.AppendFormat("-s {0}x{1} ", (int)Resolution.Width, (int)Resolution.Height);
            }
            if (Fps > 0)
            {
                argsBuilder.AppendFormat("-r {0} ", Fps);
            }
            if (VideoBitrate > 0)
            {
                argsBuilder.AppendFormat("-b:v {0}k ", VideoBitrate);
            }

            // Preset
            argsBuilder.AppendFormat("-preset {0} ", Preset.ToString().ToLower());

            // Audio options
            if (!string.IsNullOrWhiteSpace(AudioCodec))
            {
                argsBuilder.AppendFormat("-c:a {0} ", AudioCodec);
            }
            if (AudioSampleRate > 0)
            {
                argsBuilder.AppendFormat("-ar {0} ", AudioSampleRate);
            }
            if (AudioBitrate > 0)
            {
                argsBuilder.AppendFormat("-b:a {0}k ", AudioBitrate);
            }
            argsBuilder.AppendFormat("-ac {0} ", (int)AudioChannels);

            // Threads
            argsBuilder.AppendFormat("-threads {0} ", Threads);

            // Output file
            argsBuilder.AppendFormat("\"{0}\"", OutputFile);

            args = argsBuilder.ToString().Trim();
            return true;
        }

        internal static bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(InputFile) || !File.Exists(InputFile))
            {
                MessageBox.Show("Input file is invalid or does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                MessageBox.Show("Output file is not defined.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate cut times
            if (CutEnd != TimeSpan.Zero && CutEnd <= CutStart)
            {
                MessageBox.Show("End time must be after start time.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (Duration > 0 && CutEnd > TimeSpan.FromSeconds(Duration))
            {
                MessageBox.Show("End time cannot exceed the total duration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate resolution and bitrate
            if (Resolution.Width < 0 || Resolution.Height < 0)
            {
                MessageBox.Show("Resolution is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (VideoBitrate > 0 && (Resolution.Width * Resolution.Height > 1920 * 1080) && VideoBitrate < 1000)
            {
                MessageBox.Show("Video bitrate is too low for FullHD or higher resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate FPS
            if (Fps <= 0)
            {
                MessageBox.Show("FPS must be greater than 0.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate video codec
            if (string.IsNullOrWhiteSpace(VideoCodec))
            {
                MessageBox.Show("Video codec must be specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate audio codec
            if (string.IsNullOrWhiteSpace(AudioCodec))
            {
                MessageBox.Show("Audio codec must be specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate audio bitrate
            if (AudioBitrate < 0)
            {
                MessageBox.Show("Audio bitrate must be greater than or equal to 0.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate audio sample rate
            if (AudioSampleRate <= 0)
            {
                MessageBox.Show("Audio sample rate must be greater than 0.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Validate preset and threads
            if ((Preset == FfmpegPreset.Veryslow || Preset == FfmpegPreset.Placebo) && Threads < 2)
            {
                MessageBox.Show("It is recommended to use multiple threads with 'veryslow' or 'placebo' preset.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return true;
        }
    }

    internal static class Codecs
    {
        internal static Dictionary<string, string> Video => new Dictionary<string, string>
        {
            { "H.264", "libx264" },
            { "H.265", "libx265" },
            { "VP8", "libvpx" },
            { "VP9", "libvpx-vp9" },
            { "AV1", "libaom-av1" },
            { "MPEG-4", "mpeg4" },
            { "ProRes", "prores" },
            { "DNxHD", "dnxhd" },
            { "Theora", "libtheora" },
            { "MJPEG", "mjpeg" }
        };

        internal static Dictionary<string, string> Audio => new Dictionary<string, string>
        {
            { "AAC", "aac" },
            { "MP3", "libmp3lame" },
            { "Opus", "libopus" },
            { "Vorbis", "libvorbis" },
            { "AC3", "ac3" },
            { "FLAC", "flac" },
            { "PCM", "pcm_s16le" },
            { "ALAC", "alac" },
            { "WMA", "wmav2" },
            { "EAC3", "eac3" }
        };
    }

    internal enum FfmpegPreset
    {
        Ultrafast, // Bigger size
        Superfast,
        Veryfast,
        Faster,
        Fast,
        Medium, // Default
        Slow,
        Slower,
        Veryslow,
        Placebo // Smaller size
    }

    internal enum AudioChannels
    {
        Mono = 1,        // 1 Channel (Mono)
        Stereo = 2,      // 2 Channels (Stereo)
        Surround_2_1 = 3, // 2.1 Surround (Stereo + Subwoofer)
        Surround_4_0 = 4, // 4.0 Surround
        Surround_5_1 = 6, // 5.1 Surround
        Surround_7_1 = 8  // 7.1 Surround
    }
}
