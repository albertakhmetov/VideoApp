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
namespace VideoApp.Core.ViewModels;

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class PlayerViewModel : ViewModel, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IServiceProvider serviceProvider;
    private readonly IApp app;
    private readonly IPlaybackService playbackService;

    private int duration, position, volume;
    private PlaybackState state;

    private ImmutableArray<TrackInfo> audioTracks = [], subtitleTracks = [];
    private int audioTrackId, subtitleTrackId;

    public PlayerViewModel(IServiceProvider serviceProvider, IApp app, IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.serviceProvider = serviceProvider.NotNull();
        this.app = app.NotNull();
        this.playbackService = playbackService.NotNull();

        playbackService
            .Duration
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Duration = x)
            .DisposeWith(disposable);

        playbackService
            .Position
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Position = x)
            .DisposeWith(disposable);

        playbackService
            .Volume
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Set(ref volume, x, nameof(Volume)))
            .DisposeWith(disposable);

        playbackService
            .State
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => State = x)
            .DisposeWith(disposable);

        playbackService
            .AudioTracks
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => AudioTracks = x)
            .DisposeWith(disposable);

        playbackService
            .SubtitleTracks
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SubtitleTracks = x)
            .DisposeWith(disposable);

        playbackService
            .AudioTrack
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => AudioTrackId = x)
            .DisposeWith(disposable);

        playbackService
            .SubtitleTrack
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SubtitleTrackId = x)
            .DisposeWith(disposable);

        OpenMediaFileCommand = this.serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(OpenMediaFileCommand));

        TogglePlaybackCommand = this.serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(TogglePlaybackCommand));

        SettingsCommand = this.serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(SettingsCommand));

        ToggleFullScreenCommand = new RelayCommand(x => this.app.SetFullScreenMode(x is bool isEnabled ? isEnabled : null));

        SkipBackCommand = new RelayCommand(_ => SkipBack());
        SkipForwardCommand = new RelayCommand(_ => SkipForward());
        AdjustVolumeCommand = new RelayCommand(x => AdjustVolume(x is int direction ? direction : 0));
        PositionCommand = new RelayCommand(x => SetPosition(x));
        ExitCommand = new RelayCommand(_ => app.Exit());

        MruListViewModel = serviceProvider.GetRequiredService<MruListViewModel>();
        PlaylistViewModel = serviceProvider.GetRequiredService<PlaylistViewModel>();
    }

    public int Duration
    {
        get => duration;
        private set => Set(ref duration, value);
    }

    public int Position
    {
        get => position;
        set => Set(ref position, value);
    }

    public int Volume
    {
        get => volume;
        set => playbackService.SetVolume(value);
    }

    public PlaybackState State
    {
        get => state;
        private set
        {
            if (Set(ref state, value))
            {
                OnPropertyChanged(nameof(StateText));
            }
        }
    }

    public string StateText => State switch
    {
        PlaybackState.Playing => "Playing",
        PlaybackState.Paused => "Paused",
        PlaybackState.Stopped => "Stopped",
        _ => ""
    };

    public ImmutableArray<TrackInfo> AudioTracks
    {
        get => audioTracks;
        private set => Set(ref audioTracks, value);
    }

    public ImmutableArray<TrackInfo> SubtitleTracks
    {
        get => subtitleTracks;
        private set => Set(ref subtitleTracks, value);
    }

    public int AudioTrackId
    {
        get => audioTrackId;
        set
        {
            if (Set(ref audioTrackId, value))
            {
                playbackService.SetAudioTrack(value);
            }
        }
    }

    public int SubtitleTrackId
    {
        get => subtitleTrackId;
        set
        {
            if (Set(ref subtitleTrackId, value))
            {
                playbackService.SetSubtitleTrack(value);
            }
        }
    }

    public CommandBase OpenMediaFileCommand { get; }

    public ICommand SettingsCommand { get; }

    public ICommand TogglePlaybackCommand { get; }

    public ICommand ToggleFullScreenCommand { get; }

    public ICommand SkipBackCommand { get; }

    public ICommand SkipForwardCommand { get; }

    public ICommand AdjustVolumeCommand { get; }

    public ICommand PositionCommand { get; }

    public ICommand ExitCommand { get; }

    public MruListViewModel MruListViewModel { get; }

    public PlaylistViewModel PlaylistViewModel { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    private async void AdjustVolume(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        var newVolume = (Volume / 5) * 5 + Math.Sign(direction) * 5;

        playbackService.SetVolume(newVolume);
        Set(ref volume, await playbackService.Volume.FirstOrDefaultAsync(), nameof(Volume));
    }

    private void SkipBack()
    {
        playbackService.SkipBack(TimeSpan.FromSeconds(10));
    }

    private void SkipForward()
    {
        playbackService.SkipForward(TimeSpan.FromSeconds(10));
    }

    private void SetPosition(object? x)
    {
        if (x is int newPosition)
        {
            playbackService.SetPosition(newPosition);
        }
    }
}
