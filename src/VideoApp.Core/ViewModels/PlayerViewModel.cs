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
    private bool isInitialized, isStopped, isPlaying;
    private int volume;

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
            .Subscribe(x => Set(ref position, x, nameof(Position)))
            .DisposeWith(disposable);

        playbackService
            .Volume
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Set(ref volume, x, nameof(Volume)))
            .DisposeWith(disposable);

        playbackService
            .State
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => IsPlaying = x == Models.PlaybackState.Playing)
            .DisposeWith(disposable);

        playbackService
            .State
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => IsStopped = x == Models.PlaybackState.Stopped)
            .DisposeWith(disposable);

        OpenMediaFileCommand = serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(OpenMediaFileCommand));

        ToggleFullScreenCommand = new RelayCommand(x => app.SetFullScreenMode(x is bool isEnabled ? isEnabled : null));

        SkipBackCommand = new RelayCommand(_ => AdjustPosition(-10));
        SkipForwardCommand = new RelayCommand(_ => AdjustPosition(+30));

        AdjustVolumeCommand = new RelayCommand(x => AdjustVolume(x is int direction ? direction : 0));
    }

    private void AdjustPosition(int delta)
    {
        var newPosition = Math.Min(Duration - 1, Math.Max(0, Position + delta * 1000));

        playbackService.SetPosition(newPosition);
        Position = newPosition;
    }

    private void AdjustVolume(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        var newVolume = Math.Max(0, Math.Min(100, (Volume / 5) * 5 + Math.Sign(direction) * 5));

        playbackService.SetVolume(newVolume);
        Volume = newVolume;
    }

    public double Duration
    {
        get => duration;
        private set => Set(ref duration, value);
    }

    public double Position
    {
        get => position;
        private set => Set(ref position, value);
    }

    public int Volume
    {
        get => volume;
        private set => Set(ref volume, value);
    }

    public bool IsInitialized
    {
        get => isInitialized;
        private set => Set(ref isInitialized, value);
    }

    public bool IsStopped
    {
        get => isStopped;
        private set => Set(ref isStopped, value);
    }

    public bool IsPlaying
    {
        get => isPlaying;
        private set => Set(ref isPlaying, value);
    }

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
