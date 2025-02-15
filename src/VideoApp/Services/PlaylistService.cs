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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

class PlaylistService : IPlaylistService
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IPlaybackService playbackService;

    private readonly BehaviorSubject<PlaylistItems> itemsSubject;
    private readonly BehaviorSubject<FileItem?> currentItemSubject;
    private readonly BehaviorSubject<bool> isFirstItemSubject, isLastItemSubject;

    public PlaylistService(IPlaybackService playbackService)
    {
        this.playbackService = playbackService.NotNull();

        itemsSubject = new BehaviorSubject<PlaylistItems>(PlaylistItems.Empty);
        currentItemSubject = new BehaviorSubject<FileItem?>(null);
        isFirstItemSubject = new BehaviorSubject<bool>(true);
        isLastItemSubject = new BehaviorSubject<bool>(true);    
        
        currentItemSubject
            .Subscribe(x => UpdateState(x))
            .DisposeWith(disposable);

        this.playbackService
            .NotNull()
            .MediaFile
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(x => UpdateCurrentItem(x))
            .DisposeWith(disposable);

        Items = itemsSubject.AsObservable();
        CurrentItem = currentItemSubject.AsObservable();
        IsFirstItem = isFirstItemSubject.AsObservable();
        IsLastItem = isLastItemSubject.AsObservable();
    }

    public IObservable<PlaylistItems> Items { get; }

    public IObservable<FileItem?> CurrentItem { get; }

    public IObservable<bool> IsFirstItem { get; }

    public IObservable<bool> IsLastItem { get; }

    public void SetCurrentItem(FileItem item)
    {
        currentItemSubject.OnNext(item);
    }

    public void SetItems(PlaylistItems items)
    {
        itemsSubject.OnNext(items);
    }

    public async Task GoPrevious()
    {
        if (currentItemSubject.Value == null)
        {
            return;
        }

        var index = itemsSubject.Value.Items.IndexOf(currentItemSubject.Value);
        if (index - 1 >= 0)
        {
            await playbackService.Load(itemsSubject.Value.Items[index - 1].FullPath);
        }
    }

    public async Task GoNext()
    {
        if (currentItemSubject.Value == null)
        {
            return;
        }

        var index = itemsSubject.Value.Items.IndexOf(currentItemSubject.Value);
        if (index + 1 < itemsSubject.Value.Items.Length)
        {
            await playbackService.Load(itemsSubject.Value.Items[index + 1].FullPath);
        }
    }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    private void UpdateCurrentItem(FileItem? nextItem)
    {
        if (nextItem == null)
        {
            itemsSubject.OnNext(PlaylistItems.Empty);
            currentItemSubject.OnNext(null);
            return;
        }

        var item = itemsSubject.Value.Items.FirstOrDefault(x => x.Equals(nextItem));

        if (item != null)
        {
            currentItemSubject.OnNext(item);
        }
        else
        {
            itemsSubject.OnNext(new PlaylistItems(0, nextItem));
            currentItemSubject.OnNext(itemsSubject.Value.Items.First());
        }
    }

    private void UpdateState(FileItem? nextItem)
    {
        var item = itemsSubject.Value.Items.FirstOrDefault(x => x.Equals(nextItem));

        if (item == null)
        {
            isFirstItemSubject.OnNext(true);
            isLastItemSubject.OnNext(true);
        }
        else
        {
            var index = itemsSubject.Value.Items.IndexOf(item);

            isFirstItemSubject.OnNext(index == 0);
            isLastItemSubject.OnNext(index == itemsSubject.Value.Items.Length - 1);
        }
    }
}
