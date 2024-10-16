using FSC.WUF;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ConvertTo2
{
    internal class MainWindow
    {
        private WindowManager _window;
        private HtmlDocument? _body;
        private HtmlDocument? _head;
        private HtmlDocument? _content;
        private string _file = string.Empty;
        private LocalHttpServer? _server;
        private Size _origResolution;
        private Ffmpeg _ffmpeg = new Ffmpeg();

        internal MainWindow(WindowManager window)
        {
            _window = window;
            Initialize();

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                _file = Environment.GetCommandLineArgs()[1];
            }
        }

        private void Initialize()
        {
            var background = new SolidColorBrush(Color.FromArgb(255, 33, 37, 41));
            _window.Titlebar = new WindowTitlebar
            {
                Background = background,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                DisableMaximize = true,
            };

            _window.Background = background;

            var html = new Html();
            html.Load("GUI.index.html");
            _window.Load(html);

            var icon = new BitmapImage();
            icon.BeginInit();
            icon.StreamSource = Assembly.GetCallingAssembly().GetManifestResourceStream("ConvertTo2.GUI.Images.icon.png");
            icon.CacheOption = BitmapCacheOption.OnLoad;
            icon.EndInit();
            icon.Freeze();
            _window.Icon = icon;

            _window.OnLoaded += WindowOnLoaded;
        }

        private async void WindowOnLoaded(object? sender, EventArgs e)
        {
            _body = _window.GetElement("body");
            _head = _window.GetElement("head");

            var layout = new Html();
            layout.Load("GUI.Layout.html");
            await _body.Append(layout);

            var nav = _window.GetElement("#nav");
            var navContent = new Html();
            navContent.Load("GUI.Navigation.html");
            await nav.Append(navContent);

            var sidebar = _window.GetElement("#sidebar");
            var sidebarContent = new Html();
            sidebarContent.Load("GUI.Sidebar.html");
            await sidebar.Append(sidebarContent);

            var fontAwesome = new Css();
            fontAwesome.Load("GUI.Style.font-awesome.css");
            await _head.Append(fontAwesome);

            var customStyle = new Css();
            customStyle.Load("GUI.Style.custom.css");
            await _head.Append(customStyle);

            _content = _window.GetElement("#content");
            await InitNavigation();
            await InitSidebar();

            if (!string.IsNullOrWhiteSpace(_file))
            {
                await LoadData();
            }
        }

        private async Task InitNavigation()
        {
            Action<HtmlDocument> hideAll = async (HtmlDocument e) =>
            {
                await _window.GetElement("#file-ctx").RemoveClass("d-block");
                await _window.GetElement("#file-ctx").AddClass("d-none");

                await _window.GetElement("#install-ctx").RemoveClass("d-block");
                await _window.GetElement("#install-ctx").AddClass("d-none");

                await _window.GetElement("#about-ctx").RemoveClass("d-block");
                await _window.GetElement("#about-ctx").AddClass("d-none");
            };

            await _window.AddEventListener("html", "click", hideAll);

            await _window.AddEventListener("#file-btn", "blur", hideAll);
            await _window.AddEventListener("#install-btn", "blur", hideAll);
            await _window.AddEventListener("#about-btn", "blur", hideAll);

            await _window.AddEventListener("#file-btn", "click", async (e) =>
            {
                if ((await _window.GetElement("#file-ctx").ClassList()).Contains("d-block"))
                {
                    return;
                }

                await Task.Delay(100);
                await _window.GetElement("#file-ctx").RemoveClass("d-none");
                await _window.GetElement("#file-ctx").AddClass("d-block");
            });
            await _window.AddEventListener("#install-btn", "click", async (e) =>
            {
                if ((await _window.GetElement("#install-ctx").ClassList()).Contains("d-block"))
                {
                    return;
                }

                await Task.Delay(100);
                await _window.GetElement("#install-ctx").RemoveClass("d-none");
                await _window.GetElement("#install-ctx").AddClass("d-block");
            });
            await _window.AddEventListener("#about-btn", "click", async (e) =>
            {
                if ((await _window.GetElement("#about-ctx").ClassList()).Contains("d-block"))
                {
                    return;
                }

                await Task.Delay(100);
                await _window.GetElement("#about-ctx").RemoveClass("d-none");
                await _window.GetElement("#about-ctx").AddClass("d-block");
            });

            await _window.AddEventListener("#about-ctx-btn", "mousedown", (e) =>
            {
                MessageBox.Show("ConvertTo2 is the next generation of ConvertTo, a wrapper for FFmpeg, the powerful console-based media conversion software. This version introduces a wide range of new features and enhancements, expanding beyond simple format conversion to offer a more versatile and comprehensive media conversion experience.\n\n© 2024 Fabian Schlüter. All rights reserved.", "About ConvertTo2", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            await _window.AddEventListener("#open-ctx-btn", "mousedown", async (e) =>
            {
                _file = OpenFileDialog();
                await LoadData();
            });

            await _window.AddEventListener("#exit-ctx-btn", "mousedown", (e) =>
            {
                Environment.Exit(0);
            });

            await _window.AddEventListener("#install-ctx-btn", "mousedown", (e) =>
            {
                RunAsAdmin("install");
            });

            await _window.AddEventListener("#uninstall-ctx-btn", "mousedown", (e) =>
            {
                RunAsAdmin("uninstall");
            });
        }

        private static void RunAsAdmin(string argument)
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            var startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = argument // Pass the parameter (install/uninstall)
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start the process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            MessageBox.Show("Selection cancelled.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return string.Empty;
        }

        private string SaveFileDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "All files (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }

            MessageBox.Show("Selection cancelled.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return string.Empty;
        }

        private async Task InitSidebar()
        {
            await ChangeSidebarBtnActive("#settings-btn");
            var firstStartContent = new Html();
            firstStartContent.Load("GUI.Settings.html");
            await _content!.InnerHtml(firstStartContent);
            await InitSettings();
            await DisableActionTab();

            await _window.AddEventListener("#settings-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#settings-btn");

                var content = new Html();
                content.Load("GUI.Settings.html");
                await _content!.InnerHtml(content);
                await InitSettings();
            });

            await _window.AddEventListener("#image-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#image-btn");

                var content = new Html();
                content.Load("GUI.Image.html");
                await _content!.InnerHtml(content);
                await InitImage();
            });

            await _window.AddEventListener("#video-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#video-btn");

                var content = new Html();
                content.Load("GUI.Video.html");
                await _content!.InnerHtml(content);
                await InitVideo();
            });

            await _window.AddEventListener("#music-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#music-btn");

                var content = new Html();
                content.Load("GUI.Music.html");
                await _content!.InnerHtml(content);
                await InitMusic();
            });

            await _window.AddEventListener("#play-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#play-btn");

                var content = new Html();
                content.Load("GUI.Play.html");
                await _content!.InnerHtml(content);
                await InitPlay();
            });
        }

        private async Task ChangeSidebarBtnActive(string id)
        {
            int sidebarBtnCount = await _window.GetElement(".sidebar-btn").Count();
            for (int i = 0; i < sidebarBtnCount; i++)
            {
                await _window.GetElement(".sidebar-btn", i).RemoveClass("active");
            }
            await _window.GetElement(id).AddClass("active");
        }

        private async Task InitSettings()
        {
            await _window.GetElement("#outputFile").Value(FfmpegData.OutputFile);
            await _window.GetElement("#threads").Value(FfmpegData.Threads.ToString());
            await _window.GetElement("#threads").Attr("max", Environment.ProcessorCount.ToString());

            await _window.AddEventListener("#folderSelect", "click", async (e) =>
            {
                string folder = SaveFileDialog();
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    await _window.GetElement("#folder").Value(folder);
                }
            });

            await _window.AddEventListener("#outputFile", "input", async (e) =>
            {
                FfmpegData.OutputFile = await _window.GetElement("#outputFile").Value();
            });

            await _window.AddEventListener("#threads", "input", async (e) =>
            {
                FfmpegData.Threads = Convert.ToInt32(await _window.GetElement("#threads").Value());
            });
        }

        private async Task InitImage()
        {
            await _window.GetElement("#resolutionWidth").Value(FfmpegData.Resolution.Width.ToString());
            await _window.GetElement("#resolutionHeight").Value(FfmpegData.Resolution.Height.ToString());
            _origResolution = FfmpegData.Resolution;

            await _window.AddEventListener("#resolutionWidth", "input", async (e) =>
            {
                try
                {
                    int newWidth = Convert.ToInt32(await _window.GetElement("#resolutionWidth").Value());
                    double aspectRatio = (double)_origResolution.Width / _origResolution.Height; // Calculate aspect ratio based on the original resolution
                    int newHeight = (int)(newWidth / aspectRatio); // Adjust height based on aspect ratio

                    FfmpegData.Resolution = new Size(newWidth, newHeight);

                    // Update the height in the UI
                    await _window.GetElement("#resolutionHeight").Value(newHeight.ToString());
                }
                catch { }
            });

            await _window.AddEventListener("#resolutionHeight", "input", async (e) =>
            {
                try
                {
                    int newHeight = Convert.ToInt32(await _window.GetElement("#resolutionHeight").Value());
                    double aspectRatio = (double)_origResolution.Width / _origResolution.Height; // Calculate aspect ratio based on the original resolution
                    int newWidth = (int)(newHeight * aspectRatio); // Adjust width based on aspect ratio

                    FfmpegData.Resolution = new Size(newWidth, newHeight);

                    // Update the width in the UI
                    await _window.GetElement("#resolutionWidth").Value(newWidth.ToString());
                }
                catch { }
            });
        }

        private async Task InitVideo()
        {
            await _window.GetElement("#resolutionWidth").Value(FfmpegData.Resolution.Width.ToString());
            await _window.GetElement("#resolutionHeight").Value(FfmpegData.Resolution.Height.ToString());
            await _window.GetElement("#fps").Value(FfmpegData.Fps.ToString());
            await _window.GetElement("#videoBitrate").Value(FfmpegData.VideoBitrate.ToString(CultureInfo.InvariantCulture));
            await _window.GetElement("#videoCodec").Value(FfmpegData.VideoCodec);
            await _window.GetElement("#cutStart").Value(FfmpegData.CutStart.ToString(@"hh\:mm\:ss\.fff"));
            await _window.GetElement("#cutEnd").Value(FfmpegData.CutEnd.ToString(@"hh\:mm\:ss\.fff"));
            await _window.GetElement("#preset").Value(FfmpegData.Preset.ToString().ToLower());
            _origResolution = FfmpegData.Resolution;

            await _window.AddEventListener("#resolutionWidth", "input", async (e) =>
            {
                try
                {
                    int newWidth = Convert.ToInt32(await _window.GetElement("#resolutionWidth").Value());
                    double aspectRatio = (double)_origResolution.Width / _origResolution.Height; // Calculate aspect ratio based on the original resolution
                    int newHeight = (int)(newWidth / aspectRatio); // Adjust height based on aspect ratio

                    FfmpegData.Resolution = new Size(newWidth, newHeight);

                    // Update the height in the UI
                    await _window.GetElement("#resolutionHeight").Value(newHeight.ToString());
                }
                catch { }
            });

            await _window.AddEventListener("#resolutionHeight", "input", async (e) =>
            {
                try
                {
                    int newHeight = Convert.ToInt32(await _window.GetElement("#resolutionHeight").Value());
                    double aspectRatio = (double)_origResolution.Width / _origResolution.Height; // Calculate aspect ratio based on the original resolution
                    int newWidth = (int)(newHeight * aspectRatio); // Adjust width based on aspect ratio

                    FfmpegData.Resolution = new Size(newWidth, newHeight);

                    // Update the width in the UI
                    await _window.GetElement("#resolutionWidth").Value(newWidth.ToString());
                }
                catch { }
            });

            await _window.AddEventListener("#fps", "input", async (e) =>
            {
                try { FfmpegData.Fps = Convert.ToDouble(await _window.GetElement("#fps").Value()); } catch { }
            });

            await _window.AddEventListener("#videoBitrate", "input", async (e) =>
            {
                try { FfmpegData.VideoBitrate = Convert.ToDouble((await _window.GetElement("#videoBitrate").Value()).Replace(',', '.')); } catch { }
            });

            await _window.AddEventListener("#videoCodec", "change", async (e) =>
            {
                try { FfmpegData.VideoCodec = await _window.GetElement("#videoCodec").Value(); } catch { }
            });

            await _window.AddEventListener("#cutStart", "input", async (e) =>
            {
                try { FfmpegData.CutStart = TimeSpan.Parse(await _window.GetElement("#cutStart").Value()); } catch { }
            });

            await _window.AddEventListener("#cutEnd", "input", async (e) =>
            {
                try { FfmpegData.CutEnd = TimeSpan.Parse(await _window.GetElement("#cutEnd").Value()); } catch { }
            });

            await _window.AddEventListener("#preset", "change", async (e) =>
            {
                try { FfmpegData.Preset = (FfmpegPreset)Enum.Parse(typeof(FfmpegPreset), await _window.GetElement("#preset").Value()); } catch { }
            });

            try
            {
                // Check if the input file is not set, but do not throw an error in that case
                if (!string.IsNullOrWhiteSpace(FfmpegData.InputFile))
                {
                    // Check if the input file exists
                    if (File.Exists(FfmpegData.InputFile))
                    {
                        // Initialize the server if it is not already running
                        if (_server == null)
                        {
                            _server = new LocalHttpServer(); // Assuming _server is a class-level variable
                        }

                        // Only start the server if it's not already running
                        if (!_server.IsRunning())
                        {
                            _server.Start(FfmpegData.InputFile); // Start the server with the input file
                        }

                        // Set the video player source to the local HTTP URL
                        await _window.GetElement("#videoPlayer").Attr("src", "http://localhost:5001/");
                    }
                    else
                    {
                        MessageBox.Show("The specified input file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                // If input is null or empty, simply skip the file-related logic and continue
            }
            catch (Exception ex)
            {
                // Catch any unexpected errors and show a message box
                MessageBox.Show($"An error occurred while loading the video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitMusic()
        {
            // #audioCodec, #audioBitrate, #audioSampleRate, #audioChannels, #cutStart, #cutEnd
            await _window.GetElement("#audioCodec").Value(FfmpegData.AudioCodec);
            await _window.GetElement("#audioBitrate").Value(FfmpegData.AudioBitrate.ToString(CultureInfo.InvariantCulture));
            await _window.GetElement("#audioSampleRate").Value(FfmpegData.AudioSampleRate.ToString());
            await _window.GetElement("#audioChannels").Value(((int)FfmpegData.AudioChannels).ToString());
            await _window.GetElement("#cutStart").Value(FfmpegData.CutStart.ToString(@"hh\:mm\:ss\.fff"));
            await _window.GetElement("#cutEnd").Value(FfmpegData.CutEnd.ToString(@"hh\:mm\:ss\.fff"));

            await _window.AddEventListener("#audioCodec", "change", async (e) =>
            {
                try { FfmpegData.AudioCodec = await _window.GetElement("#audioCodec").Value(); } catch { }
            });

            await _window.AddEventListener("#audioBitrate", "input", async (e) =>
            {
                try { FfmpegData.AudioBitrate = Convert.ToDouble((await _window.GetElement("#audioBitrate").Value()).Replace(',', '.')); } catch { }
            });

            await _window.AddEventListener("#audioSampleRate", "input", async (e) =>
            {
                try { FfmpegData.AudioSampleRate = Convert.ToInt32(await _window.GetElement("#audioSampleRate").Value()); } catch { }
            });

            await _window.AddEventListener("#audioChannels", "change", async (e) =>
            {
                try { FfmpegData.AudioChannels = (AudioChannels)int.Parse(await _window.GetElement("#audioChannels").Value()); } catch { }
            });

            await _window.AddEventListener("#cutStart", "input", async (e) =>
            {
                try { FfmpegData.CutStart = TimeSpan.Parse(await _window.GetElement("#cutStart").Value()); } catch { }
            });

            await _window.AddEventListener("#cutEnd", "input", async (e) =>
            {
                try { FfmpegData.CutEnd = TimeSpan.Parse(await _window.GetElement("#cutEnd").Value()); } catch { }
            });

            try
            {
                // Check if the input file is not set, but do not throw an error in that case
                if (!string.IsNullOrWhiteSpace(FfmpegData.InputFile))
                {
                    // Check if the input file exists
                    if (File.Exists(FfmpegData.InputFile))
                    {
                        // Initialize the server if it is not already running
                        if (_server == null)
                        {
                            _server = new LocalHttpServer(); // Assuming _server is a class-level variable
                        }

                        // Only start the server if it's not already running
                        if (!_server.IsRunning())
                        {
                            _server.Start(FfmpegData.InputFile); // Start the server with the input file
                        }

                        // Set the video player source to the local HTTP URL
                        await _window.GetElement("#audioPlayer").Attr("src", "http://localhost:5001/");
                    }
                    else
                    {
                        MessageBox.Show("The specified input file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                // If input is null or empty, simply skip the file-related logic and continue
            }
            catch (Exception ex)
            {
                // Catch any unexpected errors and show a message box
                MessageBox.Show($"An error occurred while loading the video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitPlay()
        {
            await _window.AddEventListener("#startProcess", "click", async (e) =>
            {
                await StartConvert();
            });

            await _window.AddEventListener("#stopProcess", "click", async (e) =>
            {
                await StopConvert();
            });

            await _window.AddEventListener("#done", "click", async (e) =>
            {
                Environment.Exit(0);
            });
        }

        private async Task StartConvert()
        {
            await _window.GetElement("#startProcess").AddClass("d-none");
            await _window.GetElement("#startProcess").RemoveClass("d-block");
            await _window.GetElement("#stopProcess").AddClass("d-block");
            await _window.GetElement("#stopProcess").RemoveClass("d-none");

            await _window.GetElement("#loader").AddClass("d-block");
            await _window.GetElement("#loader").RemoveClass("d-none");

            int sidebarBtnCount = await _window.GetElement(".sidebar-btn").Count();
            for (int i = 0; i < sidebarBtnCount; i++)
            {
                await _window.GetElement(".sidebar-btn", i).AddClass("disabled");
            }

            await _window.GetElement("#file-btn").AddClass("disabled");
            await _window.GetElement("#install-btn").AddClass("disabled");
            await _window.GetElement("#about-btn").AddClass("disabled");


            bool success = FfmpegData.CreateArgs(out string args);
            if (!success)
            {
                MessageBox.Show("An error occurred during validation of all settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                await StopConvert();
                return;
            }
            _ffmpeg = new Ffmpeg();
            _ffmpeg.ProcessCompleted += OnFfmpegProcessCompleted;
            await _ffmpeg.StartAsync(args, FfmpegData.OutputFile);
        }

        private async Task StopConvert()
        {
            _ffmpeg.ProcessCompleted -= OnFfmpegProcessCompleted;
            _ffmpeg.Stop(FfmpegData.OutputFile);
            await _window.GetElement("#stopProcess").AddClass("d-none");
            await _window.GetElement("#stopProcess").RemoveClass("d-block");
            await _window.GetElement("#startProcess").AddClass("d-block");
            await _window.GetElement("#startProcess").RemoveClass("d-none");

            await _window.GetElement("#loader").AddClass("d-none");
            await _window.GetElement("#loader").RemoveClass("d-block");

            int sidebarBtnCount = await _window.GetElement(".sidebar-btn").Count();
            for (int i = 0; i < sidebarBtnCount; i++)
            {
                await _window.GetElement(".sidebar-btn", i).RemoveClass("disabled");
            }

            await _window.GetElement("#file-btn").RemoveClass("disabled");
            await _window.GetElement("#install-btn").RemoveClass("disabled");
            await _window.GetElement("#about-btn").RemoveClass("disabled");
        }

        private async void OnFfmpegProcessCompleted(object sender, bool success)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (success)
                {
                    await _window.GetElement("#loader").AddClass("d-none");
                    await _window.GetElement("#loader").RemoveClass("d-block");
                    await _window.GetElement("#done").RemoveClass("disabled");
                    await _window.GetElement("#stopProcess").AddClass("d-none");
                    await _window.GetElement("#stopProcess").RemoveClass("d-block");
                }
                else
                {
                    MessageBox.Show("An error occurred while converting the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    await StopConvert();
                }
            });
        }

        private async Task EnableActionTab()
        {
            await _window.GetElement(".sidebar-btn", 4).RemoveClass("disabled");
        }

        private async Task DisableActionTab()
        {
            await _window.GetElement(".sidebar-btn", 4).AddClass("disabled");
        }

        private  async Task LoadData()
        {
            FfmpegData.InputFile = _file;
            var mediaType = FfProbe.LoadData();
            FfmpegData.CurrentMedia = mediaType;

            switch (mediaType)
            {
                case MediaTypes.Video:
                    await _window.GetElement(".sidebar-btn", 1).AddClass("d-none"); // Image (Hide)
                    await _window.GetElement(".sidebar-btn", 1).RemoveClass("d-block"); // Image (Hide)
                    await _window.GetElement(".sidebar-btn", 2).AddClass("d-block"); // Video (Show)
                    await _window.GetElement(".sidebar-btn", 2).RemoveClass("d-none"); // Video (Show)
                    await _window.GetElement(".sidebar-btn", 3).AddClass("d-block"); // Audio (Show)
                    await _window.GetElement(".sidebar-btn", 3).RemoveClass("d-none"); // Audio (Show)
                    break;
                case MediaTypes.Audio:
                    await _window.GetElement(".sidebar-btn", 1).AddClass("d-none"); // Image (Hide)
                    await _window.GetElement(".sidebar-btn", 1).RemoveClass("d-block"); // Image (Hide)
                    await _window.GetElement(".sidebar-btn", 2).AddClass("d-none"); // Video (Hide)
                    await _window.GetElement(".sidebar-btn", 2).RemoveClass("d-block"); // Video (Hide)
                    await _window.GetElement(".sidebar-btn", 3).AddClass("d-block"); // Audio (Show)
                    await _window.GetElement(".sidebar-btn", 3).RemoveClass("d-none"); // Audio (Show)
                    break;
                case MediaTypes.Image:
                    await _window.GetElement(".sidebar-btn", 1).AddClass("d-block"); // Image (Show)
                    await _window.GetElement(".sidebar-btn", 1).RemoveClass("d-none"); // Image (Show)
                    await _window.GetElement(".sidebar-btn", 2).AddClass("d-none"); // Video (Hide)
                    await _window.GetElement(".sidebar-btn", 2).RemoveClass("d-block"); // Video (Hide)
                    await _window.GetElement(".sidebar-btn", 3).AddClass("d-none"); // Audio (Hide)
                    await _window.GetElement(".sidebar-btn", 3).RemoveClass("d-block"); // Audio (Hide)
                    break;
                case MediaTypes.None:
                    return;
            }

            FfmpegData.OutputFile = Path.Combine(Path.GetDirectoryName(FfmpegData.InputFile) ?? string.Empty, Path.GetFileNameWithoutExtension(FfmpegData.InputFile) + "_converted" + Path.GetExtension(FfmpegData.InputFile));

            await ChangeSidebarBtnActive("#settings-btn");
            var firstStartContent = new Html();
            firstStartContent.Load("GUI.Settings.html");
            await _content!.InnerHtml(firstStartContent);
            await EnableActionTab();
            await InitSettings();
        }
    }
}
