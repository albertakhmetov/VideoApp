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

using VideoApp.Core;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;
using VideoApp.Services;
using Windows.Storage.Pickers;

public class OpenMediaFileCommand : CommandBase
{
    private readonly IApp app;
    private readonly IPlaylistService playlistService;
    private readonly IPlaybackService playbackService;

    public OpenMediaFileCommand(IApp app, IPlaylistService playlistService, IPlaybackService playbackService)
    {
        this.app = app.NotNull();
        this.playlistService = playlistService.NotNull();
        this.playbackService = playbackService.NotNull();
    }

    public override async void Execute(object? parameter)
    {
        if (parameter is IEnumerable<string> list && list.Any())
        {
            var playlist = new PlaylistItems(0, [.. list.Select(x => new FileItem(x))]);
            playlistService.SetItems(playlist);

            await playbackService.Load(playlist.Items.First().FullPath);

        }
        else if (parameter is string filePath && File.Exists(filePath))
        {
            await playbackService.Load(filePath);
        }
        else
        {
            var openPicker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, app.WindowHandle);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".mkv");
            openPicker.FileTypeFilter.Add(".avi");
            openPicker.FileTypeFilter.Add(".mp4");

            var files = await openPicker.PickMultipleFilesAsync();
            if (files != null && files.Any())
            {
                var playlist = new PlaylistItems(0, [.. files.Select(x => new FileItem(x.Path))]);
                playlistService.SetItems(playlist);

                await playbackService.Load(playlist.Items.First().FullPath);
            }
        }
    }
}
