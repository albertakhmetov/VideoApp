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
using VideoApp.Core.Services;

public class StartupViewModel : ViewModel, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IPlaybackService playbackService;
    private bool isActivePlayback;

    public StartupViewModel(IServiceProvider serviceProvider, IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.playbackService = playbackService.NotNull();

        playbackService
            .State
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => IsActivePlayback = x == Models.PlaybackState.Playing || x == Models.PlaybackState.Paused)
            .DisposeWith(disposable);
    }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    public bool IsActivePlayback
    {
        get => isActivePlayback;
        private set => Set(ref isActivePlayback, value);
    }
}
