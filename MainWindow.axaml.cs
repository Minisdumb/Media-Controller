using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using SharpHook;
using SharpHook.Data;

namespace MusicController
{
    public partial class MainWindow : Window
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private TaskPoolGlobalHook? _keyboardHook;
        

        private DispatcherTimer? _animationTimer;
        private double _targetOpacity = 0.0;
        private const double AnimationSpeed = 0.15;


        private DispatcherTimer? _timelineTimer;
        private bool _isSongPlaying = false;
        private TimeSpan _lastWindowsPosition = TimeSpan.Zero;
        private DateTime _lastWindowsUpdate = DateTime.MinValue;
        private TimeSpan _songDuration = TimeSpan.Zero;


        private bool _isCtrlPressed = false;
        private bool _isAltPressed = false;
        private bool _isShiftPressed = false;

         public MainWindow()
        {
            InitializeComponent();
            InitializeMediaSession();
            InitializeAnimationTimer();
            InitializeTimelineTimer();
            
            this.Topmost = App.TopmostEnabled;
            ApplyOverlayMode();

            Opened += MainWindow_Opened;
            PointerPressed += MainWindow_PointerPressed;
            PositionChanged += MainWindow_PositionChanged; 

            InitializeGlobalKeyboardHook();
        }

        
        private void MainWindow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            
            if (App.ToggleableOverlayEnabled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void MainWindow_PositionChanged(object? sender, PixelPointEventArgs e)
        {
            if (App.ToggleableOverlayEnabled && Bounds.Width > 0)
            {
        
                App.SavedX = Position.X;
                App.SavedY = Position.Y;
                App.HasSavedPosition = true;
                App.SaveSettings(); 
            }
        }
        public void ApplyOverlayMode()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (App.ToggleableOverlayEnabled)
                {
                    this.IsVisible = true;
                    this.IsHitTestVisible = true;
                    this.Opacity = 1.0;
                    _animationTimer?.Stop();
                    if (App.HasSavedPosition) Position = new Avalonia.PixelPoint(App.SavedX, App.SavedY);
                }
                else
                {
                    this.Opacity = 0.0;
                    this.IsHitTestVisible = false;
                    this.IsVisible = false;
                    if (App.HasSavedPosition) Position = new Avalonia.PixelPoint(App.SavedX, App.SavedY);
                }
            });
        }

        private void InitializeAnimationTimer()
        {
            _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void InitializeTimelineTimer()
        {
            _timelineTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timelineTimer.Tick += TimelineTimer_Tick;
            _timelineTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (Math.Abs(this.Opacity - _targetOpacity) > 0.01)
            {
                if (this.Opacity < _targetOpacity)
                    this.Opacity = Math.Min(1.0, this.Opacity + AnimationSpeed);
                else
                    this.Opacity = Math.Max(0.0, this.Opacity - AnimationSpeed);
            }
            else
            {
                this.Opacity = _targetOpacity;
                _animationTimer?.Stop();

                if (_targetOpacity == 0.0)
                {
                    this.IsHitTestVisible = false; 
                    this.IsVisible = false;        
                }
            }
        }

        private void TimelineTimer_Tick(object? sender, EventArgs e)
        {
            if (_sessionManager == null) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (_isSongPlaying && _songDuration.TotalSeconds > 0)
                {
                    TimeSpan realTimePassed = DateTime.Now - _lastWindowsUpdate;
                    TimeSpan estimatedPosition = _lastWindowsPosition + realTimePassed;

                    if (estimatedPosition > _songDuration)
                        estimatedPosition = _songDuration;

                    TimelineSlider.Maximum = _songDuration.TotalSeconds;
                    TimelineSlider.Value = estimatedPosition.TotalSeconds;

                    TxtCurrentTime.Text = FormatTime(estimatedPosition.TotalSeconds);
                    TxtTotalTime.Text = FormatTime(_songDuration.TotalSeconds);
                }
            });
        }

        private string FormatTime(double totalSeconds)
        {
            if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds) || totalSeconds < 0)
                return "0:00";

            TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
            if (time.TotalHours >= 1)
            {
                return time.ToString(@"h\:mm\:ss");
            }
            return time.ToString(@"m\:ss");
        }

        private void InitializeGlobalKeyboardHook()
        {
            _keyboardHook = new TaskPoolGlobalHook();
            _keyboardHook.KeyPressed += OnGlobalKeyPressed;
            _keyboardHook.KeyReleased += OnGlobalKeyReleased;
            Task.Run(() => _keyboardHook.Run());
        }

        private void OnGlobalKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            if (App.ToggleableOverlayEnabled) return;

            if (e.Data.KeyCode == KeyCode.VcLeftControl || e.Data.KeyCode == KeyCode.VcRightControl) _isCtrlPressed = true;
            if (e.Data.KeyCode == KeyCode.VcLeftAlt || e.Data.KeyCode == KeyCode.VcRightAlt) _isAltPressed = true;
            if (e.Data.KeyCode == KeyCode.VcLeftShift || e.Data.KeyCode == KeyCode.VcRightShift) _isShiftPressed = true;

            if (e.Data.KeyCode == App.CurrentKeybind)
            {
                if (_isCtrlPressed == App.RequireCtrl && _isAltPressed == App.RequireAlt && _isShiftPressed == App.RequireShift)
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                        this.IsVisible = true;
                        this.IsHitTestVisible = true;
                        _targetOpacity = 1.0;

                        if (!App.AnimationsEnabled) this.Opacity = 1.0;
                        else _animationTimer?.Start();
                    });
                }
            }
        }

        private void OnGlobalKeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            if (App.ToggleableOverlayEnabled) return;

            if (e.Data.KeyCode == KeyCode.VcLeftControl || e.Data.KeyCode == KeyCode.VcRightControl) _isCtrlPressed = false;
            if (e.Data.KeyCode == KeyCode.VcLeftAlt || e.Data.KeyCode == KeyCode.VcRightAlt) _isAltPressed = false;
            if (e.Data.KeyCode == KeyCode.VcLeftShift || e.Data.KeyCode == KeyCode.VcRightShift) _isShiftPressed = false;

            if (e.Data.KeyCode == App.CurrentKeybind)
            {
                Dispatcher.UIThread.Post(() => 
                {
                    _targetOpacity = 0.0;

                    if (!App.AnimationsEnabled)
                    {
                        this.Opacity = 0.0;
                        this.IsHitTestVisible = false;
                        this.IsVisible = false;
                    }
                    else _animationTimer?.Start();
                });
            }
        }

        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (App.HasSavedPosition)
                {
                    Position = new Avalonia.PixelPoint(App.SavedX, App.SavedY);
                }
                else
                {
                    var screens = Screens.ScreenFromWindow(this);
                    if (screens != null)
                    {
                        var workingArea = screens.WorkingArea;
                        int x = workingArea.X + workingArea.Width - (int)this.Bounds.Width - 100; 
                        int y = workingArea.Y + workingArea.Height - (int)this.Bounds.Height - 200;
                        
                        App.SavedX = x;
                        App.SavedY = y;
                        Position = new Avalonia.PixelPoint(x, y);
                    }
                }
            }, DispatcherPriority.Background);
        }

        private async void InitializeMediaSession()
        {
            try
            {
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                UpdateCurrentSession();
            }
            catch (Exception ex)
            {
                TxtTitle.Text = "Error";
                TxtArtist.Text = ex.Message;
            }
        }

        private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentSession();
        }

        private void UpdateCurrentSession()
        {
            var currentSession = _sessionManager?.GetCurrentSession();
            if (currentSession != null)
            {
                currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
                
                FetchMediaProperties(currentSession);
                FetchPlaybackInfo(currentSession);
                FetchTimelineProperties(currentSession);
            }
            else
            {
                _isSongPlaying = false;
                UpdateUI("No playback active", "No active media player found", false, null);
            }
        }

        private async void FetchMediaProperties(GlobalSystemMediaTransportControlsSession currentSession)
        {
            var properties = await currentSession.TryGetMediaPropertiesAsync();
            if (properties != null)
            {
                var playbackInfo = currentSession.GetPlaybackInfo();
                _isSongPlaying = playbackInfo != null && playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                Avalonia.Media.Imaging.Bitmap? bitmap = null;
                if (properties.Thumbnail != null)
                {
                    try
                    {
                        using (var stream = await properties.Thumbnail.OpenReadAsync())
                        using (var ms = new MemoryStream())
                        {
                            await stream.AsStreamForRead().CopyToAsync(ms);
                            ms.Position = 0;
                            bitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                    }
                }
catch
 { 

 }
}

 FetchTimelineProperties(currentSession);
 UpdateUI(properties.Title, properties.Artist, _isSongPlaying, bitmap);
 }
 }
 private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
 {FetchPlaybackInfo(sender);FetchTimelineProperties(sender);
 }
 private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
 {
    FetchTimelineProperties(sender);
    }
    private void FetchTimelineProperties(GlobalSystemMediaTransportControlsSession currentSession)
    {
        var timeline = currentSession.GetTimelineProperties();
        if (timeline != null){_songDuration = timeline.EndTime;
        _lastWindowsPosition = timeline.Position;_lastWindowsUpdate = DateTime.Now;
        }
        }
        private void FetchPlaybackInfo(GlobalSystemMediaTransportControlsSession currentSession)
        {
        var playbackInfo = currentSession.GetPlaybackInfo();
        if (playbackInfo != null){_isSongPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        if (_isSongPlaying){
            _lastWindowsUpdate = DateTime.Now;
            }
            Dispatcher.UIThread.Post(() =>
            {
                IconPlayPause.Symbol = _isSongPlaying ? FluentIcons.Common.Symbol.Pause : FluentIcons.Common.Symbol.Play;
            }
            );
            }
            }
            private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
            {
                FetchMediaProperties(sender);
                }
                private void UpdateUI(string title, string artist, bool isPlaying, Avalonia.Media.Imaging.Bitmap? bitmap)
                {
                    Dispatcher.UIThread.Post(() =>
        {
                    TxtTitle.Text = string.IsNullOrEmpty(title) ? "Unknown Title" : title;
                    TxtArtist.Text = string.IsNullOrEmpty(artist) ? "Unknown Artist" : artist;
                    IconPlayPause.Symbol = isPlaying ? FluentIcons.Common.Symbol.Pause : FluentIcons.Common.Symbol.Play;
                    ImgThumbnail.Source = bitmap;
                    if (!isPlaying && _sessionManager?.GetCurrentSession() == null)
                    {TimelineSlider.Value = 0;
                    TxtCurrentTime.Text = "0:00";
                    TxtTotalTime.Text = "0:00";
                    _songDuration = TimeSpan.Zero;
                    }
                    }
                    );
                    }
                    private async void BtnPlayPause_Click(object? sender, RoutedEventArgs e)
                    {
                        var session = _sessionManager?.GetCurrentSession();
                        if (session != null) await session.TryTogglePlayPauseAsync();
                        }
                        private async void BtnPrev_Click(object? sender, RoutedEventArgs e)
                        {
                        var session = _sessionManager?.GetCurrentSession();
                        if (session != null) await session.TrySkipPreviousAsync();
                        }
                    private async void BtnNext_Click(object? sender, RoutedEventArgs e)
                    {
                    var session = _sessionManager?.GetCurrentSession();
                    if (session != null) await session.TrySkipNextAsync();
                    }
                    protected override void OnClosed(EventArgs e){
                    _keyboardHook?.Dispose();
                    _timelineTimer?.Stop();
                    base.OnClosed(e);
                    }
                }
 }