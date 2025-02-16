/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of VideoApp.
 *
 *  VideoApp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  VideoApp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with VideoApp. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace VideoApp;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibVLCSharp.Platforms.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.Services;
using VideoApp.Core.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Win32.Foundation;

public sealed partial class MainWindow : Window
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private IDisposable? notificationDisposable;

    private readonly ISystemEventsService systemEventsService;
    private readonly ISettingsService settingsService;
    private readonly IPlaybackService playbackService;

    private bool resumePlaybackAfterSettings;

    public MainWindow(
        IServiceProvider serviceProvider,
        ISystemEventsService systemEventsService,
        ISettingsService settingsService,
        IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        ViewModel = serviceProvider.GetRequiredService<PlayerViewModel>();
        SettingsViewModel = serviceProvider.GetRequiredService<SettingsViewModel>();

        this.systemEventsService = systemEventsService.NotNull();
        this.settingsService = settingsService.NotNull();
        this.playbackService = playbackService.NotNull();

        this.InitializeComponent();

        this.settingsService
            .UI
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ToggleSettings(x))
            .DisposeWith(disposable);

        this.settingsService
            .Theme
            .CombineLatest(this.systemEventsService.DarkTheme.Select(x => x))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateTheme(x.First == AppTheme.System ? x.Second : (x.First == AppTheme.Dark)))
            .DisposeWith(disposable);


        var activity = Observable.Merge(
            Observable.FromEventPattern<PointerRoutedEventArgs>(Root, nameof(Control.PointerMoved)).Select(_ => true),
            playbackService.State.Where(x => x == PlaybackState.Loading).Select(_ => true));

        var inactivity = activity
            .Throttle(TimeSpan.FromSeconds(2))
            .StartWith(false);

        activity
            .Select(_ => false)
            .Merge(inactivity)
            .ObserveOn(SynchronizationContext.Current)
            .Select(x => x && ViewModel.State == PlaybackState.Playing && !ControlLayer.IsActive)
            .Subscribe(x => UpdateControlLayer(x))
            .DisposeWith(disposable);

        Root.Unloaded += OnUnloaded;

        Root.IsTabStop = false;

        Root.AllowDrop = true;
        Root.DragOver += OnDragOver;
        Root.Drop += OnDrop;

        Root.DoubleTapped += OnDoubleTapped;
        Root.PreviewKeyDown += OnPreviewKeyDown;
    }

    public PlayerViewModel ViewModel { get; private set; }

    public SettingsViewModel SettingsViewModel { get; private set; }

    private async void ToggleSettings(bool isVisible)
    {
        var state = await playbackService.State.FirstAsync();

        if (isVisible)
        {
            resumePlaybackAfterSettings = state == PlaybackState.Playing;
            if (resumePlaybackAfterSettings)
            {
                playbackService.Pause();
            }

            PLAYER.Visibility = Visibility.Collapsed;
            SETTINGS.Visibility = Visibility.Visible;
        }
        else
        {
            PLAYER.Visibility = Visibility.Visible;
            SETTINGS.Visibility = Visibility.Collapsed;

            if (resumePlaybackAfterSettings)
            {
                playbackService.Play();
            }
        }
    }

    private unsafe void UpdateTheme(bool isDarkTheme)
    {
        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));

        var isDark = (uint)(isDarkTheme ? 1 : 0);

        var result = Windows.Win32.PInvoke.DwmSetWindowAttribute(
            hwnd: hwnd,
            dwAttribute: Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            pvAttribute: Unsafe.AsPointer(ref isDark),
            cbAttribute: sizeof(uint));

        if (result != 0)
        {
            throw Marshal.GetExceptionForHR(result) ?? throw new ApplicationException("Can't switch dark mode setting");
        }

        if (Content is FrameworkElement element)
        {
            element.RequestedTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private void VideoView_Initialized(object sender, InitializedEventArgs e)
    {
        playbackService.Initialize(sender, e.SwapChainOptions);
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();

            ViewModel?.OpenMediaFileCommand.Execute(items.Where(x => x is StorageFile).Select(x => ((StorageFile)x).Path));
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = "Play";
        e.DragUIOverride.IsGlyphVisible = false;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.Dispose();
        Bindings.StopTracking();

        playbackService.Pause();

        Root.ShowCursor();        

        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }

        notificationDisposable?.Dispose();
        notificationDisposable = null;
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel?.State == PlaybackState.Playing && e.PointerDeviceType == PointerDeviceType.Touch)
        {
            var position = e.GetPosition(Root);

            if (position.X < Root.ActualWidth / 6)
            {
                SkipPosition(-1);
            }
            else if (position.X > Root.ActualWidth * 5 / 6)
            {
                SkipPosition(+1);
            }
            else
            {
                ViewModel.ToggleFullScreenCommand.Execute(null);
            }
        }
        else
        {
            ViewModel?.ToggleFullScreenCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            HideControlLayer();
        }

        e.Handled = ProcessKey(e);
    }

    private bool ProcessKey(KeyRoutedEventArgs e)
    {
        if (e.OriginalSource is VideoView == false && e.OriginalSource is Control control && control.FocusState == FocusState.Keyboard)
        {
            return false;
        }

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Up:
                UpdateVolume(+1);
                return true;

            case Windows.System.VirtualKey.Down:
                UpdateVolume(-1);
                return true;

            case Windows.System.VirtualKey.Left:
                SkipPosition(-1);
                return true;

            case Windows.System.VirtualKey.Right:
                SkipPosition(+1);
                return true;

            case Windows.System.VirtualKey.Escape:
                ViewModel?.ToggleFullScreenCommand.Execute(false);
                return true;

            case Windows.System.VirtualKey.Space:
                TogglePlaybackState();
                return true;


            default:
                return false;
        }
    }

    private void UpdateVolume(int direction)
    {
        notificationDisposable?.Dispose();

        notificationDisposable = Observable
            .Timer(TimeSpan.FromSeconds(1))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => VisualStateManager.GoToState(NotificationLayer, "None", true));

        if (direction < 0)
        {
            ViewModel?.PlaybackViewModel.DecreaseVolumeCommand.Execute(null);
        }
        else
        {
            ViewModel?.PlaybackViewModel.IncreaseVolumeCommand.Execute(null);
        }

        VisualStateManager.GoToState(NotificationLayer, "Volume", true);
    }

    private void TogglePlaybackState()
    {
        if (ViewModel?.State != PlaybackState.Paused && ViewModel?.State != PlaybackState.Playing)
        {
            return;
        }

        notificationDisposable?.Dispose();

        notificationDisposable = Observable
            .Timer(TimeSpan.FromSeconds(1))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => VisualStateManager.GoToState(NotificationLayer, "None", true));

        ViewModel.PlaybackViewModel.TogglePlaybackCommand.Execute(null);
        VisualStateManager.GoToState(NotificationLayer, "PlaybackState", true);
    }

    private void SkipPosition(int direction)
    {
        notificationDisposable?.Dispose();

        notificationDisposable = Observable
            .Timer(TimeSpan.FromSeconds(1))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => VisualStateManager.GoToState(NotificationLayer, "None", true));

        if (direction < 0)
        {
            ViewModel?.PlaybackViewModel?.SkipBackCommand.Execute(null);
            VisualStateManager.GoToState(NotificationLayer, "Rewind", true);
        }
        else
        {
            ViewModel?.PlaybackViewModel?.SkipForwardCommand.Execute(null);
            VisualStateManager.GoToState(NotificationLayer, "Forward", true);
        }
    }

    private void UpdateControlLayer(bool hide)
    {
        if (hide)
        {
            HideControlLayer();
        }
        else
        {
            ShowControlLayer();
        }
    }

    private void ShowControlLayer()
    {
        if (VisualStateManager.GoToState(ControlLayer, "Visible", true))
        {
            Root.ShowCursor();
        }
    }

    private void HideControlLayer()
    {
        if (VisualStateManager.GoToState(ControlLayer, "Hidden", true))
        {
            ControlLayer.Focus(FocusState.Programmatic);

            Root.HideCursor();
        }
    }
}
