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
namespace VideoApp.Controls;

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.ViewModels;

public partial class PlayerToolbarControl : UserControl
{
    public static DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(PlayerViewModel),
        typeof(PlayerToolbarControl),
        new PropertyMetadata(null, OnViewModelChanged));

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PlayerToolbarControl control)
        {
            if (e.OldValue is PlayerViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= control.ViewModel_PropertyChanged;
            }

            if (e.NewValue is PlayerViewModel newViewModel)
            {
                newViewModel.PropertyChanged += control.ViewModel_PropertyChanged;
            }

            if (e.NewValue is PlayerViewModel viewModel)
            {
                control.Bindings.Update();

                control.RebuildTracksMenu(viewModel.AudioTracks, viewModel.AudioTrackId, 0);
                control.RebuildTracksMenu(viewModel.SubtitleTracks, viewModel.SubtitleTrackId, 1);
            }
            else
            {
                control.Bindings.StopTracking();
            }

        }
    }

    private static readonly string[] TrackGroups = { "Audio", "Subtitle" };

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(PlayerViewModel.AudioTracks):
                RebuildTracksMenu(ViewModel.AudioTracks, ViewModel.AudioTrackId, 0);
                break;

            case nameof(PlayerViewModel.SubtitleTracks):
                RebuildTracksMenu(ViewModel.SubtitleTracks, ViewModel.SubtitleTrackId, 1);
                break;

            case nameof(PlayerViewModel.AudioTrackId):
                UpdateMenuSelection(ViewModel.AudioTrackId, 0);
                break;

            case nameof(PlayerViewModel.SubtitleTrackId):
                UpdateMenuSelection(ViewModel.SubtitleTrackId, 1);
                break;
        }
    }

    private void RebuildTracksMenu(IList<TrackInfo> tracks, int trackId, int groupIndex)
    {
        var toRemove = TracksFlyout.Items
            .Where(x => x is RadioMenuFlyoutItem i && i.GroupName == TrackGroups[groupIndex])
            .ToArray();

        foreach (var i in toRemove)
        {
            if (i is RadioMenuFlyoutItem item)
            {
                item.Click -= TrackMenuClick;
            }

            TracksFlyout.Items.Remove(i);
        }

        var separator = TracksFlyout.Items
            .Skip(groupIndex)
            .Where(x => x is MenuFlyoutSeparator)
            .First();
        var index = TracksFlyout.Items.IndexOf(separator);
        separator.Visibility = tracks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var i in tracks)
        {
            var item = new RadioMenuFlyoutItem
            {
                Text = i.Text,
                DataContext = i,
                GroupName = TrackGroups[groupIndex]
            };

            item.Click += TrackMenuClick;

            TracksFlyout.Items.Insert(++index, item);
        }

        UpdateMenuSelection(trackId, groupIndex);
    }

    private void TrackMenuClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        if (sender is RadioMenuFlyoutItem item && item.DataContext is TrackInfo track)
        {
            if (item.GroupName == TrackGroups[0])
            {
                ViewModel.AudioTrackId = track.Id;
            }
            else if (item.GroupName == TrackGroups[1])
            {
                ViewModel.SubtitleTrackId = track.Id;
            }
        }
    }

    private void UpdateMenuSelection(int trackId, int groupIndex)
    {
        var items = TracksFlyout.Items
           .Select(x => x as RadioMenuFlyoutItem)
           .Where(x => x != null && x.GroupName == TrackGroups[groupIndex]);

        foreach (var i in items)
        {
            i!.IsChecked = i.DataContext is TrackInfo track && track.Id == trackId;
        }
    }

    public PlayerToolbarControl()
    {
        this.InitializeComponent();

        PointerEntered += (_, _) => ContainsPointer = true;
        PointerExited += (_, _) => ContainsPointer = false;

        Tapped += (_, e) => e.Handled = true;
        DoubleTapped += (_, e) => e.Handled = true;
    }

    public PlayerViewModel? ViewModel
    {
        get => (PlayerViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public bool ContainsPointer { get; private set; }

    public bool IsFlyoutOpen => TracksFlyout.IsOpen || VolumeFlyout.IsOpen || MenuFlyout.IsOpen;
}
