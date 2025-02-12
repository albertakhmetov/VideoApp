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
using VideoApp.Core.Services;
using VideoApp.Services;
using Windows.Storage.Pickers;

public class OpenMediaFileCommand : CommandBase
{
    private readonly IApp app;
    private readonly IPlaybackService playbackService;

    public OpenMediaFileCommand(IApp app, IPlaybackService playbackService)
    {
        this.app = app.NotNull();
        this.playbackService = playbackService.NotNull();
    }

    public override async void Execute(object? parameter)
    {
        if (parameter is IList<string> list && list.Count > 0)
        {
            await playbackService.Load(list.First());

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

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                playbackService.Load(file.Path);
            }
        }
    }
}
