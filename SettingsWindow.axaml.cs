using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.IO;

namespace MusicController
{
    public partial class SettingsWindow : Window
    {
        private bool _isListeningForKey = false;
        private Button? _keybindButton;
        private ToggleButton? _toggleCtrl;
        private ToggleButton? _toggleAlt;
        private ToggleButton? _toggleShift;

        public SettingsWindow()
        {
            InitializeComponent();

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new WindowIcon(iconPath);
            }

            var toggleAnim = this.FindControl<ToggleButton>("ToggleAnim");
            if (toggleAnim != null) toggleAnim.IsChecked = App.AnimationsEnabled;

            var toggleOverlay = this.FindControl<ToggleButton>("ToggleOverlayMode");
            if (toggleOverlay != null) toggleOverlay.IsChecked = App.ToggleableOverlayEnabled;

            var resetPositionBtn = this.FindControl<Button>("ResetPosition");
            _toggleCtrl = this.FindControl<ToggleButton>("ToggleCtrl");
            _toggleAlt = this.FindControl<ToggleButton>("ToggleAlt");
            _toggleShift = this.FindControl<ToggleButton>("ToggleShift");
            _keybindButton = this.FindControl<Button>("BtnKeybind");

            if (_toggleCtrl != null) _toggleCtrl.IsChecked = App.RequireCtrl;
            if (_toggleAlt != null) _toggleAlt.IsChecked = App.RequireAlt;
            if (_toggleShift != null) _toggleShift.IsChecked = App.RequireShift;

            if (_keybindButton != null)
            {
                _keybindButton.Content = App.CurrentKeybind.ToString().Replace("Vc", "");
            }

            PointerPressed += SettingsWindow_PointerPressed;
            KeyDown += SettingsWindow_KeyDown;
        }

        private void SettingsWindow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }



        private void ToggleOverlayMode_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleBtn)
            {
                App.ToggleableOverlayEnabled = toggleBtn.IsChecked ?? false;
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is MainWindow mainWin)
                {
                    mainWin.ApplyOverlayMode();
                }
                App.SaveSettings();
            }
        }

        private void Modifier_Click(object? sender, RoutedEventArgs e)
        {
            App.RequireCtrl = _toggleCtrl?.IsChecked ?? false;
            App.RequireAlt = _toggleAlt?.IsChecked ?? false;
            App.RequireShift = _toggleShift?.IsChecked ?? false;
            App.SaveSettings();
        }

        private void BtnKeybind_Click(object? sender, RoutedEventArgs e)
        {
            _isListeningForKey = true;
            if (_keybindButton != null)
            {
                _keybindButton.Content = "Press key...";
                _keybindButton.Foreground = Avalonia.Media.Brushes.Orange;
            }
        }

        private void SettingsWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_isListeningForKey) return;

            string avaloniaKeyName = e.Key.ToString();
            string sharpHookKeyName = "Vc" + avaloniaKeyName;

            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) sharpHookKeyName = "VcLeftControl";
            else if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) sharpHookKeyName = "VcLeftAlt";
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift) sharpHookKeyName = "VcLeftShift";

            if (Enum.TryParse(typeof(SharpHook.Data.KeyCode), sharpHookKeyName, out var targetCode))
            {
                App.CurrentKeybind = (SharpHook.Data.KeyCode)targetCode;
                _isListeningForKey = false;

                if (_keybindButton != null)
                {
                    _keybindButton.Content = avaloniaKeyName.Replace("Left", "").Replace("Right", "");
                    _keybindButton.Foreground = Avalonia.Media.Brushes.LimeGreen;
                }
                App.SaveSettings();
            }
            e.Handled = true;
        }

        private void ToggleAnim_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleBtn)
            {
                App.AnimationsEnabled = toggleBtn.IsChecked ?? false;
                App.SaveSettings();
            }
        }
        private void ResetPosition_Click(object? sender, RoutedEventArgs e)
        {
            App.SavedX = 0;
            App.SavedY = 0;
            App.SaveSettings();

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is MainWindow mainWin)
            {
                mainWin.Position = new Avalonia.PixelPoint(0, 0);
            }
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
