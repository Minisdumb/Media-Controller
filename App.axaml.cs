using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace MusicController
{
    public partial class App : Application
    {
        public static bool AutostartEnabled { get; set; } = false;

        public static bool AnimationsEnabled { get; set; } = true;
        public static bool TopmostEnabled { get; set; } = true;
        public static bool ToggleableOverlayEnabled { get; set; } = false;
        public static SharpHook.Data.KeyCode CurrentKeybind { get; set; } = SharpHook.Data.KeyCode.VcSpace;
        public static bool RequireCtrl { get; set; } = true;
        public static bool RequireAlt { get; set; } = true;
        public static bool RequireShift { get; set; } = false;

        public static int SavedX { get; set; } = -1;
        public static int SavedY { get; set; } = -1;
        public static bool HasSavedPosition { get; set; } = false;

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private TrayIcon? _trayIcon;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            LoadSettings();
            ApplyWindowsAutostart();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();

                string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
                WindowIcon? trayWindowIcon = null;
                if (File.Exists(iconPath))
                {
                    trayWindowIcon = new WindowIcon(iconPath);
                }

                _trayIcon = new TrayIcon
                {
                    Icon = trayWindowIcon,
                    ToolTipText = "Music Controller"
                };

                var menu = new NativeMenu();
                var settingsItem = new NativeMenuItem("Settings");
                settingsItem.Click += MenuSettings_Click;
                
                var exitItem = new NativeMenuItem("Exit");
                exitItem.Click += MenuExit_Click;

                menu.Items.Add(settingsItem);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(exitItem);

                _trayIcon.Menu = menu;

                var icons = new TrayIcons { _trayIcon };
                TrayIcon.SetIcons(this, icons);
            }

            base.OnFrameworkInitializationCompleted();
        }
        public static void ApplyWindowsAutostart()
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    if (key != null)
                    {
                        string exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                        
                        if (AutostartEnabled)
                        {
                            key.SetValue("MusicControllerWidget", $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue("MusicControllerWidget", false);
                        }
                    }
                }
            }
            catch { }
        }
        public static void SaveSettings()
        {
            try
            {
                var data = new SettingsData
                {
                    AnimationsEnabled = App.AnimationsEnabled,
                    TopmostEnabled = App.TopmostEnabled,
                    ToggleableOverlayEnabled = App.ToggleableOverlayEnabled,
                    CurrentKeybind = (int)App.CurrentKeybind,
                    RequireCtrl = App.RequireCtrl,
                    RequireAlt = App.RequireAlt,
                    RequireShift = App.RequireShift,
                    SavedX = App.SavedX,
                    SavedY = App.SavedY,
                    HasSavedPosition = App.HasSavedPosition
                };

                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, jsonString);
            }
            catch { }
        }

        private static void LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;

                string jsonString = File.ReadAllText(ConfigPath);
                var data = JsonSerializer.Deserialize<SettingsData>(jsonString);

                if (data != null)
                {
                    App.AnimationsEnabled = data.AnimationsEnabled;
                    App.TopmostEnabled = data.TopmostEnabled;
                    App.ToggleableOverlayEnabled = data.ToggleableOverlayEnabled;
                    App.CurrentKeybind = (SharpHook.Data.KeyCode)data.CurrentKeybind;
                    App.RequireCtrl = data.RequireCtrl;
                    App.RequireAlt = data.RequireAlt;
                    App.RequireShift = data.RequireShift;
                    App.SavedX = data.SavedX;
                    App.SavedY = data.SavedY;
                    App.HasSavedPosition = data.HasSavedPosition;
                }
            }
            catch { }
        }

        public void MenuSettings_Click(object? sender, EventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Show(); 
        }

        public void MenuExit_Click(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _trayIcon?.Dispose(); 
                desktop.Shutdown();   
            }
        }
    }

    public class SettingsData
    {
        public bool AnimationsEnabled { get; set; }
        public bool TopmostEnabled { get; set; }
        public bool ToggleableOverlayEnabled { get; set; }
        public int CurrentKeybind { get; set; }
        public bool RequireCtrl { get; set; }
        public bool RequireAlt { get; set; }
        public bool RequireShift { get; set; }
        public int SavedX { get; set; }
        public int SavedY { get; set; }
        public bool HasSavedPosition { get; set; }
    }
}
