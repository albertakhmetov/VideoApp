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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class PlaybackViewModel : ViewModel, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IPlaybackService playbackService;
    private readonly IPlaylistService playlistService;
    private readonly ISettingsService settingsService;

    private int duration, position, volume;
    private bool isStopped, isLoading, isPlaying, isPaused;
    private bool canGoPrevious, canGoNext;
    private bool isRemainingTimeEnabled;

    public PlaybackViewModel(
        IServiceProvider serviceProvider, 
        IPlaybackService playbackService, 
        IPlaylistService playlistService,
        ISettingsService settingsService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.playbackService = playbackService.NotNull();
        this.playlistService = playlistService.NotNull();
        this.settingsService = settingsService.NotNull();

        this.playbackService
            .Duration
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Duration = x)
            .DisposeWith(disposable);

        this.playbackService
            .Position
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Position = x)
            .DisposeWith(disposable);

        this.playbackService
            .State
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateState(x))
            .DisposeWith(disposable);

        this.playbackService
            .Volume
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Volume = x)
            .DisposeWith(disposable);

        this.playlistService
            .IsFirstItem
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => CanGoPrevious = !x)
            .DisposeWith(disposable);

        this.playlistService
            .IsLastItem
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => CanGoNext = !x)
            .DisposeWith(disposable);

        this.settingsService
            .RemainingTime
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => IsRemainingTimeEnabled = x)
            .DisposeWith(disposable);

        RemainingTimeCommand = new RelayCommand(_ => this.settingsService.SetRemainingTime(!IsRemainingTimeEnabled));

        TogglePlaybackCommand = serviceProvider
            .NotNull()
            .GetRequiredKeyedService<CommandBase>(nameof(TogglePlaybackCommand));

        GoPreviousCommand = new RelayCommand(_ => this.playlistService.GoPrevious());
        GoNextCommand = new RelayCommand(_ => this.playlistService.GoNext());

        PositionCommand = new RelayCommand(x => SetPosition(x));
        SkipBackCommand = new RelayCommand(_ => this.playbackService.SkipBack(TimeSpan.FromSeconds(10)));
        SkipForwardCommand = new RelayCommand(_ => this.playbackService.SkipForward(TimeSpan.FromSeconds(10)));

        VolumeCommand = new RelayCommand(x => SetVolume(x));
        DecreaseVolumeCommand = new RelayCommand(_ => this.playbackService.DecreaseValue(5));
        IncreaseVolumeCommand = new RelayCommand(_ => this.playbackService.IncreaseVolume(5));
    }

    public int Duration
    {
        get => duration;
        private set
        {
            if (Set(ref duration, value))
            {
                OnPropertyChanged(nameof(RemainingTime));
            }
        }
    }

    public int RemainingTime
    {
        get => duration - position;
    }

    public int Position
    {
        get => position;
        private set
        {
            if (Set(ref position, value))
            {
                OnPropertyChanged(nameof(RemainingTime));
            }
        }
    }

    public int Volume
    {
        get => volume;
        private set => Set(ref volume, value);
    }

    public bool CanGoPrevious
    {
        get => canGoPrevious && (IsActivePlayback || IsStopped);
        private set => Set(ref canGoPrevious, value);
    }

    public bool CanGoNext
    {
        get => canGoNext && (IsActivePlayback || IsStopped);
        private set => Set(ref canGoNext, value);
    }

    public bool IsStopped
    {
        get => isStopped;
        private set => Set(ref isStopped, value);
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => Set(ref isLoading, value);
    }

    public bool IsActivePlayback => IsPaused || IsPlaying;

    public bool IsPlaying
    {
        get => isPlaying;
        private set
        {
            if (Set(ref isPlaying, value))
            {
                OnPropertyChanged(nameof(IsActivePlayback));
            }
        }
    }

    public bool IsPaused
    {
        get => isPaused;
        private set
        {
            if (Set(ref isPaused, value))
            {
                OnPropertyChanged(nameof(IsActivePlayback));
            }
        }
    }

    public bool IsRemainingTimeEnabled
    {
        get => isRemainingTimeEnabled;
        private set => Set(ref isRemainingTimeEnabled, value);
    }

    public ICommand RemainingTimeCommand { get; }

    public ICommand TogglePlaybackCommand { get; }

    public ICommand GoPreviousCommand { get; }

    public ICommand GoNextCommand { get; }

    public ICommand PositionCommand { get; }

    public ICommand SkipBackCommand { get; }

    public ICommand SkipForwardCommand { get; }

    public ICommand VolumeCommand { get; }

    public ICommand DecreaseVolumeCommand { get; }

    public ICommand IncreaseVolumeCommand { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    private void UpdateState(PlaybackState x)
    {
        IsStopped = x == PlaybackState.Stopped;
        IsLoading = x == PlaybackState.Loading;
        IsPlaying = x == PlaybackState.Playing;
        IsPaused = x == PlaybackState.Paused;

        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    private void SetPosition(object? x)
    {
        if (x is int newPosition)
        {
            playbackService.SetPosition(newPosition);
        }
    }

    private void SetVolume(object? x)
    {
        if (x is int newVolume)
        {
            playbackService.SetVolume(newVolume);
        }
    }
}
