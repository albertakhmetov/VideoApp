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

public class PlayerControlViewModel : ViewModel, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IServiceProvider serviceProvider;
    private readonly IApp app;
    private readonly IPlaybackService playbackService;

    private double duration, position;
    private PlaybackState state;
    private int volume;

    private ImmutableArray<TrackInfo> audioTracks = [], subtitleTracks = [];
    private TrackInfo? subtitleTrack;
    private TrackInfo? audioTrack;

    public PlayerControlViewModel(IServiceProvider serviceProvider, IApp app, IPlaybackService playbackService)
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
            .Subscribe(x => AudioTrack = AudioTracks.FirstOrDefault(i => i.Id == x))
            .DisposeWith(disposable);

        playbackService
            .SubtitleTrack
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SubtitleTrack = SubtitleTracks.FirstOrDefault(i => i.Id == x))
            .DisposeWith(disposable);

        OpenMediaFileCommand = serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(OpenMediaFileCommand));

        TogglePlaybackCommand = serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(TogglePlaybackCommand));

        ToggleFullScreenCommand = new RelayCommand(x => app.SetFullScreenMode(x is bool isEnabled ? isEnabled : null));

        SkipBackCommand = new RelayCommand(_ => playbackService.SkipBack(TimeSpan.FromSeconds(10)));
        SkipForwardCommand = new RelayCommand(_ => playbackService.SkipForward(TimeSpan.FromSeconds(30)));
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

    public int Volume
    {
        get => volume;
        set => playbackService.SetVolume(value);
    }

    public PlaybackState State
    {
        get => state;
        private set => Set(ref state, value);
    }

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

    public CommandBase OpenMediaFileCommand { get; }

    public ICommand TogglePlaybackCommand { get; }

    public ICommand ToggleFullScreenCommand { get; }

    public ICommand SkipBackCommand { get; }

    public ICommand SkipForwardCommand { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }
}
