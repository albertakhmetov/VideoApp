﻿/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
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

    private readonly IApp app;
    private readonly IPlaybackService playbackService;

    private double duration, position;
    private PlaybackState state;
    private int volume;

    public PlayerViewModel(
        IServiceProvider serviceProvider,
        IApp app,
        IPlaybackService playbackService,
        PlayerControlViewModel playerControlViewModel)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.app = app.NotNull();
        this.playbackService = playbackService.NotNull();

        PlayerControlViewModel = playerControlViewModel.NotNull();

        playbackService
            .Duration
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Duration = x)
            .DisposeWith(disposable);

        playbackService
            .Position
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Set(ref position, x, nameof(Position)))
            .DisposeWith(disposable);

        playbackService
            .Volume
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Set(ref volume, x, nameof(Volume)))
            .DisposeWith(disposable);

        playbackService
            .State
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => State = x)
            .DisposeWith(disposable);

        OpenMediaFileCommand = serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(OpenMediaFileCommand));

        TogglePlaybackCommand = serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(TogglePlaybackCommand));

        ToggleFullScreenCommand = new RelayCommand(x => app.SetFullScreenMode(x is bool isEnabled ? isEnabled : null));

        SkipBackCommand = new RelayCommand(_ => playbackService.SkipBack(TimeSpan.FromSeconds(10)));
        SkipForwardCommand = new RelayCommand(_ => playbackService.SkipForward(TimeSpan.FromSeconds(30)));
        AdjustVolumeCommand = new RelayCommand(x => AdjustVolume(x is int direction ? direction : 0));
    }

    public PlayerControlViewModel PlayerControlViewModel { get; }

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

    public CommandBase OpenMediaFileCommand { get; }

    public ICommand TogglePlaybackCommand { get; }

    public ICommand ToggleFullScreenCommand { get; }

    public ICommand SkipBackCommand { get; }

    public ICommand SkipForwardCommand { get; }

    public ICommand AdjustVolumeCommand { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

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
}
