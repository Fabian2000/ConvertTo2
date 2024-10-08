﻿using FSC.WUF;
using System.Reflection;
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
        internal MainWindow(WindowManager window)
        {
            _window = window;
            Initialize();
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

            _content = _window.GetElement("#content");
            await InitSidebar();
        }

        private async Task InitSidebar()
        {
            await ChangeSidebarBtnActive("#settings-btn");

            await _window.AddEventListener("#settings-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#settings-btn");

                var content = new Html();
                content.Load("GUI.Settings.html");
                await _content!.InnerHtml(content);
            });

            await _window.AddEventListener("#image-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#image-btn");

                var content = new Html();
                content.Load("GUI.Image.html");
                await _content!.InnerHtml(content);
            });

            await _window.AddEventListener("#video-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#video-btn");

                var content = new Html();
                content.Load("GUI.Video.html");
                await _content!.InnerHtml(content);
            });

            await _window.AddEventListener("#music-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#music-btn");

                var content = new Html();
                content.Load("GUI.Music.html");
                await _content!.InnerHtml(content);
            });

            await _window.AddEventListener("#play-btn", "click", async (e) =>
            {
                await ChangeSidebarBtnActive("#play-btn");

                var content = new Html();
                content.Load("GUI.Play.html");
                await _content!.InnerHtml(content);
            });
        }


        private async Task ChangeSidebarBtnActive(string id)
        {
            for (int i = 0; i < 5; i++)
            {
                await _window.GetElement(".sidebar-btn", i).RemoveClass("active");
            }
            await _window.GetElement(id).AddClass("active");
        }
    }
}