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
using System.Collections.Immutable;
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
                oldViewModel.MruListViewModel.ItemSelected -= control.MruViewModel_ItemSelected;
                oldViewModel.PlaylistViewModel.ItemSelected -= control.PlaylistViewModel_ItemSelected;
                oldViewModel.TracksViewModel.ItemSelected -= control.TracksViewModel_ItemSelected;
            }

            if (e.NewValue is PlayerViewModel viewModel)
            {
                viewModel.MruListViewModel.ItemSelected += control.MruViewModel_ItemSelected;
                viewModel.PlaylistViewModel.ItemSelected += control.PlaylistViewModel_ItemSelected;
                viewModel.TracksViewModel.ItemSelected += control.TracksViewModel_ItemSelected;

                control.Bindings.Update();
            }
            else
            {
                control.Bindings.StopTracking();
            }
        }
    }

    private void TracksViewModel_ItemSelected(object? sender, EventArgs e)
    {
        TracksFlyout.Hide();
    }

    private void MruViewModel_ItemSelected(object? sender, EventArgs e)
    {
        MruFlyout.Hide();
    }

    private void PlaylistViewModel_ItemSelected(object? sender, EventArgs e)
    {
        PlaylistFlyout.Hide();
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

    public bool IsFlyoutOpen => TracksFlyout.IsOpen || VolumeFlyout.IsOpen || MenuFlyout.IsOpen || MruFlyout.IsOpen || PlaylistFlyout.IsOpen;
}
