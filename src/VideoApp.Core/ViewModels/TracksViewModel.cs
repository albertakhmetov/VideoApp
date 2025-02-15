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

using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class TracksViewModel : ViewModel
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IPlaybackService playbackService;
    private int audioTrackId, subtitleTrackId;

    public TracksViewModel(IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        AudioTracks = [];
        SubtitleTracks = [];

        this.playbackService = playbackService.NotNull();

        playbackService
            .AudioTracks
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SetAudioTracks(x))
            .DisposeWith(disposable);

        playbackService
            .SubtitleTracks
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SetSubtitleTracks(x))
            .DisposeWith(disposable);

        playbackService
            .AudioTrack
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => AudioTrackId = x)
            .DisposeWith(disposable);

        playbackService
            .SubtitleTrack
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => SubtitleTrackId = x)
            .DisposeWith(disposable);
    }

    public bool HasTracks => AudioTracks.Count + SubtitleTracks.Count > 0;

    public AppObservableCollection<Item> AudioTracks { get; }

    public AppObservableCollection<Item> SubtitleTracks { get; }

    public int AudioTrackId
    {
        get => audioTrackId;
        private set
        {
            if (Set(ref audioTrackId, value))
            {
                UpdateAudioState();
            }
        }
    }

    public int SubtitleTrackId
    {
        get => subtitleTrackId;
        private set
        {
            if (Set(ref subtitleTrackId, value))
            {
                UpdateSubtitleState();
            }
        }
    }

    public event EventHandler<EventArgs>? ItemSelected;

    public void Dispose()
    {
        disposable.Dispose();
    }

    private void SelectItem(Item? item)
    {
        if (item != null)
        {
            if (AudioTracks.Contains(item))
            {
                playbackService.SetAudioTrack(item.Id);
            }
            else if (SubtitleTracks.Contains(item))
            {
                playbackService.SetSubtitleTrack(item.Id);
            }
        }

        ItemSelected?.Invoke(this, EventArgs.Empty);
    }

    private void SetAudioTracks(ImmutableArray<TrackInfo> items)
    {
        AudioTracks.Set(items.Select(x => new Item(this, x)));
        UpdateAudioState();
        OnPropertyChanged(nameof(HasTracks));
    }

    private void SetSubtitleTracks(ImmutableArray<TrackInfo> items)
    {
        SubtitleTracks.Set(items.Select(x => new Item(this, x)));
        UpdateSubtitleState();
        OnPropertyChanged(nameof(HasTracks));
    }

    private void UpdateAudioState()
    {
        foreach (var track in AudioTracks)
        {
            track.IsSelected = track.Id == AudioTrackId;
        }
    }

    private void UpdateSubtitleState()
    {
        foreach (var track in SubtitleTracks)
        {
            track.IsSelected = track.Id == SubtitleTrackId;
        }
    }

    public sealed class Item : ObservableObject
    {
        private readonly TracksViewModel owner;
        private bool isSelected;

        public Item(TracksViewModel owner, TrackInfo trackInfo)
        {
            this.owner = owner.NotNull();

            Id = trackInfo.NotNull().Id;
            Text = trackInfo.NotNull().Text;

            SelectCommand = new RelayCommand(x => this.owner.SelectItem(this));
        }

        public int Id { get; }

        public string Text { get; }

        public bool IsSelected
        {
            get => isSelected;
            set => Set(ref isSelected, value);
        }

        public ICommand SelectCommand { get; }
    }
}
