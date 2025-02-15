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

    private readonly BehaviorSubject<PlaylistItems> itemsSubject;
    private readonly BehaviorSubject<FileItem?> currentItemSubject;

    public PlaylistService(IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        playbackService
            .NotNull()
            .MediaFile
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateCurrentItem(x))
            .DisposeWith(disposable);

        itemsSubject = new BehaviorSubject<PlaylistItems>(PlaylistItems.Empty);
        currentItemSubject = new BehaviorSubject<FileItem?>(null);

        Items = itemsSubject.AsObservable();
        CurrentItem = currentItemSubject.AsObservable();
    }

    public IObservable<PlaylistItems> Items { get; }

    public IObservable<FileItem?> CurrentItem { get; }

    public void SetCurrentItem(FileItem item)
    {
        currentItemSubject.OnNext(item);
    }

    public void SetItems(PlaylistItems items)
    {
        itemsSubject.OnNext(items);
    }

    public void Dispose()
    {
        disposable.Dispose();
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
}
