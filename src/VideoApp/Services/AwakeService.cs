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
namespace VideoApp.Services;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using VideoApp.Core;
using VideoApp.Core.Services;
using Windows.Win32;
using Windows.Win32.System.Power;

class AwakeService : IDisposable
{
    private IPlaybackService playbackService;

    private CompositeDisposable disposable = new CompositeDisposable();
    private IDisposable? awakeTimer;

    public AwakeService(IPlaybackService playbackService)
    {
        this.playbackService = playbackService.NotNull();
    }

    public void Dispose()
    {
        awakeTimer?.Dispose();
        disposable.Dispose();
    }

    public void Start()
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        playbackService
             .State
             .Where(x => x == Core.Models.PlaybackState.Playing)
             .ObserveOn(SynchronizationContext.Current)
             .Subscribe(_ => StartAwakeTimer())
             .DisposeWith(disposable);

        playbackService
            .State
            .Where(x => x != Core.Models.PlaybackState.Playing)
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => StopAwakeTimer())
            .DisposeWith(disposable);
    }

    public void Stop()
    {
        StopAwakeTimer();
    }

    private void StartAwakeTimer()
    {
        if (awakeTimer == null)
        {
            awakeTimer = Observable
                .Timer(TimeSpan.FromSeconds(4))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(_ =>
                {
                    PInvoke.SetThreadExecutionState(
                        EXECUTION_STATE.ES_DISPLAY_REQUIRED |
                        EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                        EXECUTION_STATE.ES_CONTINUOUS);
                });
        }
    }

    private void StopAwakeTimer()
    {
        if (awakeTimer != null)
        {
            awakeTimer?.Dispose();
            awakeTimer = null;

            PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }
    }
}
