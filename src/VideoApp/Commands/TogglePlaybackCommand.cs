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
namespace VideoApp.Commands;

using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using VideoApp.Core;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class TogglePlaybackCommand : CommandBase
{
    private readonly IPlaybackService playbackService;
    private readonly CommandBase openMediaFileCommand;

    public TogglePlaybackCommand(IServiceProvider serviceProvider, IPlaybackService playbackService)
    {
        this.playbackService = playbackService.NotNull();

        serviceProvider.NotNull();
        
        openMediaFileCommand = serviceProvider.GetRequiredKeyedService<CommandBase>(nameof(OpenMediaFileCommand));
    }

    public async override void Execute(object? parameter)
    {
        if (!playbackService.IsInitialized)
        {
            return;
        }

        var state = await playbackService.State.FirstOrDefaultAsync();

        if (state == PlaybackState.Closed)
        {
            openMediaFileCommand.Execute(null);
        }
        else if (state == PlaybackState.Stopped)
        {
            playbackService.Play();
        }
        else
        {
            playbackService.TooglePlaying();
        }
    }
}
