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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharpDX.Mathematics.Interop;
using VideoApp.Core;
using VideoApp.Core.Services;
using VideoApp.Core.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

public sealed partial class PlayerView : UserControl
{
    private readonly IPlaybackService playbackService;

    private CompositeDisposable? disposable;
    private IDisposable? notificationDisposable;

    public PlayerView(IPlaybackService playbackService, PlayerViewModel viewModel)
    {
        this.playbackService = playbackService.NotNull();

        this.InitializeComponent();

        ViewModel = viewModel.NotNull();

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

            var file = items.FirstOrDefault() as StorageFile;

            if (file != null)
            {
                ViewModel.OpenMediaFileCommand.Execute(file.Path);
            }
        }
    }

    private void PlayerView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
    }

    private bool IsFlyoutOpen => TracksFlyout.IsOpen || VolumeFlyout.IsOpen || MenuFlyout.IsOpen;

    private void PlayerView_Loaded(object sender, RoutedEventArgs e)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        disposable = new CompositeDisposable();

        Observable
            .FromEventPattern<PointerRoutedEventArgs>(ControlPanel, nameof(Control.PointerEntered))
            .Subscribe(_ => inTheBar = true)
            .DisposeWith(disposable);

        Observable
            .FromEventPattern<PointerRoutedEventArgs>(ControlPanel, nameof(Control.PointerExited))
            .Subscribe(_ => inTheBar = false)
            .DisposeWith(disposable);

        var activity = Observable.FromEventPattern<PointerRoutedEventArgs>(this, nameof(Control.PointerMoved));

        activity
            .Subscribe(_ => VisualStateManager.GoToState(this, "Normal", true))
            .DisposeWith(disposable);

        activity
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current)
            .Where(_ => !inTheBar && !IsFlyoutOpen)
            .Subscribe(_ => NewMethod())
            .DisposeWith(disposable);

        Bindings.Initialize();
    }

    private bool NewMethod()
    {
        return VisualStateManager.GoToState(this, "HiddenControlPanel", true);
    }

    private bool inTheBar;

    private void PlayerView_Unloaded(object sender, RoutedEventArgs e)
    {
        Bindings.StopTracking();

        disposable?.Dispose();
        disposable = null;

        notificationDisposable?.Dispose();
        notificationDisposable = null;
    }

    public PlayerViewModel ViewModel { get; }

    private void VideoView_Initialized(object sender, LibVLCSharp.Platforms.Windows.InitializedEventArgs e)
    {
        playbackService.Initialize(sender, e.SwapChainOptions);
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        ViewModel.ToggleFullScreenCommand.Execute(null);
        e.Handled = true;
    }

    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        e.Handled = ProcessKey(e);

        base.OnKeyDown(e);
    }

    private bool ProcessKey(KeyRoutedEventArgs e)
    {
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
                ViewModel.ToggleFullScreenCommand.Execute(false);
                return true;

            case Windows.System.VirtualKey.Space:
                if (e.OriginalSource is Control control == false || control.FocusState == FocusState.Unfocused)
                {
                    ViewModel.TogglePlaybackCommand.Execute(null);
                    return true;
                }
                else
                {
                    return false;
                }

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

        ViewModel.AdjustVolumeCommand.Execute(direction);

        VisualStateManager.GoToState(this, "VolumeNotification", true);
    }
    private void SkipPosition(int direction)
    {
        notificationDisposable?.Dispose();

        notificationDisposable = Observable
            .Timer(TimeSpan.FromSeconds(1))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => VisualStateManager.GoToState(this, "NoNotifications", true));

        if (direction < 0)
        {
            ViewModel.SkipBackCommand.Execute(null);
            SkipOsdIcon.Glyph = "\uED3C";
        }
        else
        {
            ViewModel.SkipForwardCommand.Execute(null);
            SkipOsdIcon.Glyph = "\uED3D";
        }

        VisualStateManager.GoToState(this, "SkipNotification", true);
    }

    private void ControlPanel_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void ControlPanel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        e.Handled = true;
    }
}
