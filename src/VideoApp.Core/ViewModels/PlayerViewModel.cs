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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class PlayerViewModel : ViewModel
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IServiceProvider serviceProvider;
    private readonly IApp app;
    private readonly IPlaybackService playbackService;

    private double duration, position;
    private PlaybackState state;
    private string stateText, positionText;
    private bool isInitialized;
    private int volume;

    private ImmutableArray<TrackInfo> audioTrackInfo = [], subtitleTrackInfo = [];
    private TrackInfo? subtitleTrack;
    private TrackInfo? audioTrack;

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
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Set(ref position, x, nameof(Position)))
            .DisposeWith(disposable);

        playbackService
            .Position
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => PositionText = TimeSpan.FromSeconds(Convert.ToInt64(x / 1000)).ToString())
            .DisposeWith(disposable);

        playbackService
            .Volume
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Set(ref volume, x, nameof(Volume)))
            .DisposeWith(disposable);

        playbackService
            .State
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => State = x)
            .DisposeWith(disposable);

        playbackService
            .State
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => StateText = GetStateText(x))
            .DisposeWith(disposable);

        playbackService
            .AudioTrackInfo
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => AudioTrackInfo = x)
            .DisposeWith(disposable);

        playbackService
            .SubtitleTrackInfo
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SubtitleTrackInfo = x)
            .DisposeWith(disposable);

        playbackService
            .AudioTrack
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => AudioTrack = AudioTrackInfo.FirstOrDefault(i => i.Id == x))
            .DisposeWith(disposable);

        playbackService
            .SubtitleTrack
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SubtitleTrack = SubtitleTrackInfo.FirstOrDefault(i => i.Id == x))
            .DisposeWith(disposable);

        OpenMediaFileCommand = serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(OpenMediaFileCommand));

        TogglePlaybackCommand = new RelayCommand(_ => TooglePlayback());
        ToggleFullScreenCommand = new RelayCommand(x => app.SetFullScreenMode(x is bool isEnabled ? isEnabled : null));

        SkipBackCommand = new RelayCommand(_ => playbackService.SkipBack(TimeSpan.FromSeconds(10)));
        SkipForwardCommand = new RelayCommand(_ => playbackService.SkipForward(TimeSpan.FromSeconds(30)));
        AdjustVolumeCommand = new RelayCommand(x => AdjustVolume(x is int direction ? direction : 0));
    }

    private string GetStateText(PlaybackState x) => x switch
    {
        PlaybackState.Playing => "Playing",
        PlaybackState.Paused => "Paused",
        PlaybackState.Stopped => "Stopped",
        _ => ""
    };

    private void AdjustVolume(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        var newVolume = (Volume / 5) * 5 + Math.Sign(direction) * 5;

        playbackService.SetVolume(newVolume);
        Volume = newVolume;
    }

    private async void TooglePlayback()
    {
        if (!playbackService.IsInitialized)
        {
            return;
        }

        var state = await playbackService.State.FirstOrDefaultAsync();

        if (state == Models.PlaybackState.Closed)
        {
            OpenMediaFileCommand.Execute(null);
        }
        else if (state == Models.PlaybackState.Stopped)
        {
            playbackService.Play();
        }
        else
        {
            playbackService.TooglePlaying();
        }
    }

    public double Duration
    {
        get => duration;
        private set => Set(ref duration, value);
    }

    public double Position
    {
        get => position;
        set => playbackService.SetPosition(value);
    }

    public string PositionText
    {
        get => positionText;
        private set => Set(ref positionText, value);
    }

    public int Volume
    {
        get => volume;
        set => playbackService.SetVolume(value);
    }

    public bool IsInitialized
    {
        get => isInitialized;
        private set => Set(ref isInitialized, value);
    }

    public PlaybackState State
    {
        get => state;
        private set => Set(ref state, value);
    }

    public string StateText
    {
        get => stateText;
        private set => Set(ref stateText, value);
    }

    public ImmutableArray<TrackInfo> AudioTrackInfo
    {
        get => audioTrackInfo;
        private set => Set(ref audioTrackInfo, value);
    }

    public ImmutableArray<TrackInfo> SubtitleTrackInfo
    {
        get => subtitleTrackInfo;
        private set => Set(ref subtitleTrackInfo, value);
    }

    public TrackInfo? AudioTrack
    {
        get => audioTrack;
        set
        {
            if (value != null && Set(ref audioTrack, value))
            {
                playbackService.SetAudioTrack(value.Id);
            }
        }
    }

    public TrackInfo? SubtitleTrack
    {
        get => subtitleTrack;
        set
        {
            if (value != null && Set(ref subtitleTrack, value))
            {
                playbackService.SetSubtitleTrack(value.Id);
            }
        }
    }

    public ICommand TogglePlaybackCommand { get; }

    public ICommand ToggleFullScreenCommand { get; }

    public ICommand SkipBackCommand { get; }

    public ICommand SkipForwardCommand { get; }

    public ICommand AdjustVolumeCommand { get; }

    public CommandBase OpenMediaFileCommand { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }
}
