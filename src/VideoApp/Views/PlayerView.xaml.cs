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
namespace VideoApp.Views;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using LibVLCSharp.Platforms.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using SharpDX.Mathematics.Interop;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.Services;
using VideoApp.Core.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

public sealed partial class PlayerView : UserControl
{
    private readonly IServiceProvider serviceProvider;
    private readonly IPlaybackService playbackService;

    private CompositeDisposable? disposable;
    private IDisposable? notificationDisposable;

    public PlayerView(IServiceProvider serviceProvider, IPlaybackService playbackService)
    {
        this.serviceProvider = serviceProvider.NotNull();
        this.playbackService = playbackService.NotNull();

        this.InitializeComponent();

        this.Loaded += PlayerView_Loaded;
        this.Unloaded += PlayerView_Unloaded;

        IsTabStop = false;

        AllowDrop = true;
        DragOver += PlayerView_DragOver;
        Drop += PlayerView_Drop;
    }

    private async void PlayerView_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();

            ViewModel?.OpenMediaFileCommand.Execute(items.Where(x => x is StorageFile).Select(x => ((StorageFile)x).Path));
        }
    }

    private void PlayerView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = "Play";
        e.DragUIOverride.IsGlyphVisible = false;
    }

    private void PlayerView_Loaded(object sender, RoutedEventArgs e)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        ViewModel = serviceProvider.GetRequiredService<PlayerViewModel>();
        disposable = new CompositeDisposable();

        var activity = Observable.Merge(
            Observable.FromEventPattern<PointerRoutedEventArgs>(this, nameof(Control.PointerMoved)).Select(_ => true),
            Observable.FromEventPattern<DragEventArgs>(this, nameof(Control.DragOver)).Select(_ => true));

        activity
            .Subscribe(_ => ShowToolbar())
            .DisposeWith(disposable);

        activity
            .Throttle(TimeSpan.FromSeconds(1.2))
            .ObserveOn(SynchronizationContext.Current)
            .Where(_ => ViewModel.State == PlaybackState.Playing && !Toolbar.ContainsPointer && !Toolbar.IsFlyoutOpen)
            .Subscribe(_ => HideToolbar())
            .DisposeWith(disposable);

        Bindings.Initialize();
        Bindings.Update();
    }

    private void ShowToolbar()
    {
        if (VisualStateManager.GoToState(this, "ToolbarVisible", true))
        {
            ProtectedCursor = null;
        }
    }

    private void HideToolbar()
    {
        if (VisualStateManager.GoToState(this, "ToolbarHidden", true))
        {
            Toolbar.Focus(FocusState.Programmatic);

            if (ProtectedCursor == null)
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }

            ProtectedCursor.Dispose();
        }
    }

    private void PlayerView_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.Dispose();
        Bindings.StopTracking();

        playbackService.Pause();

        ProtectedCursor?.Dispose();

        disposable?.Dispose();
        disposable = null;

        notificationDisposable?.Dispose();
        notificationDisposable = null;
    }

    public PlayerViewModel? ViewModel { get; private set; }

    private void VideoView_Initialized(object sender, InitializedEventArgs e)
    {
        playbackService.Initialize(sender, e.SwapChainOptions);
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel?.State == PlaybackState.Playing && e.PointerDeviceType == PointerDeviceType.Touch)
        {
            var position = e.GetPosition(this);

            if (position.X < ActualWidth / 4)
            {
                SkipPosition(-1);
            }
            else if (position.X > ActualWidth * 3 / 4)
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

    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            HideToolbar();
        }

        e.Handled = ProcessKey(e);

        base.OnKeyDown(e);
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
            .Subscribe(x => VisualStateManager.GoToState(this, "NoNotifications", true));

        ViewModel?.AdjustVolumeCommand.Execute(direction);

        VisualStateManager.GoToState(this, "VolumeNotification", true);
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
            .Subscribe(x => VisualStateManager.GoToState(this, "NoNotifications", true));

        ViewModel.TogglePlaybackCommand.Execute(null);
        VisualStateManager.GoToState(this, "PlaybackStateNotification", true);
    }

    private void SkipPosition(int direction)
    {
        if (ViewModel?.State != PlaybackState.Playing)
        {
            return;
        }

        notificationDisposable?.Dispose();

        notificationDisposable = Observable
            .Timer(TimeSpan.FromSeconds(1))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => VisualStateManager.GoToState(this, "NoNotifications", true));

        if (direction < 0)
        {
            ViewModel?.SkipBackCommand.Execute(null);
            VisualStateManager.GoToState(this, "RewindNotification", true);
        }
        else
        {
            ViewModel?.SkipForwardCommand.Execute(null);
            VisualStateManager.GoToState(this, "ForwardNotification", true);
        }

    }
}
