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

    private int duration, position;
    private bool isStopped, isLoading, isPlaying, isPaused;

    public PlaybackViewModel(IServiceProvider serviceProvider, IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

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
            .State
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateState(x))
            .DisposeWith(disposable);

        TogglePlaybackCommand = serviceProvider
            .NotNull()
            .GetRequiredKeyedService<CommandBase>(nameof(TogglePlaybackCommand));
        
        SkipBackCommand = new RelayCommand(_ => SkipBack());
        SkipForwardCommand = new RelayCommand(_ => SkipForward());
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

    public ICommand TogglePlaybackCommand { get; }

    public ICommand SkipBackCommand { get; }

    public ICommand SkipForwardCommand { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    private void UpdateState(PlaybackState x)
    {
        IsStopped = x == PlaybackState.Closed
            || x == PlaybackState.Opening
            || x == PlaybackState.Stopped;

        IsLoading = x == PlaybackState.Opening;
        IsPlaying = x == PlaybackState.Playing;
        IsPaused = x == PlaybackState.Paused;
    }

    private void SkipBack()
    {
        playbackService.SkipBack(TimeSpan.FromSeconds(10));
    }

    private void SkipForward()
    {
        playbackService.SkipForward(TimeSpan.FromSeconds(10));
    }
}
