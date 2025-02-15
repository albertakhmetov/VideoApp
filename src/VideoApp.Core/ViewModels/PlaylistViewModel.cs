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

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class PlaylistViewModel : ViewModel, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IPlaylistService playlistService;
    private readonly IPlaybackService playbackService;
    private long playlistVersion;

    private FileItem? currentItem;

    public PlaylistViewModel(IPlaylistService playlistService, IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        Items = new AppObservableCollection<Item>();

        this.playbackService = playbackService.NotNull();
        this.playlistService = playlistService.NotNull();

        this.playlistService
            .Items
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SetItems(x.Items))
            .DisposeWith(disposable);

        this.playlistService
            .CurrentItem
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SetCurrentItem(x))
            .DisposeWith(disposable);
    }

    public bool HasItems => Items.Count > 0;

    public AppObservableCollection<Item> Items { get; }

    public event EventHandler<EventArgs>? ItemSelected;

    public void Dispose()
    {
        disposable.Dispose();
    }

    private void SelectItem(Item? item)
    {
        if (item != null)
        {
            playbackService.Load(item.FullPath);
        }

        ItemSelected?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveItem(Item? item)
    {
        if (item != null)
        {
            Items.Remove(item);
            OnPropertyChanged(nameof(HasItems));
        }
    }

    private void SetItems(ImmutableArray<FileItem> items)
    {
        Items.Set(items.Select(x => new Item(this, x.FullPath)));
        OnPropertyChanged(nameof(HasItems));
    }

    private void SetCurrentItem(FileItem? x)
    {
        foreach (var i in Items)
        {
            i.IsCurrent = x?.Equals(i.FullPath) == true;
        }
    }

    public sealed class Item : ObservableObject
    {
        private readonly PlaylistViewModel owner;
        private bool isCurrent;

        public Item(PlaylistViewModel owner, string fileName)
        {
            this.owner = owner.NotNull();

            FullPath = fileName;
            Directory = Path.GetDirectoryName(fileName) ?? string.Empty;
            Name = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;

            SelectCommand = new RelayCommand(x => owner.SelectItem(this));
            RemoveCommand = new RelayCommand(x => owner.RemoveItem(this));
        }

        public bool IsCurrent
        {
            get => isCurrent;
            set => Set(ref isCurrent, value);
        }

        public string FullPath { get; }

        public string Directory { get; }

        public string Name { get; }

        public ICommand SelectCommand { get; }

        public ICommand RemoveCommand { get; }
    }
}
