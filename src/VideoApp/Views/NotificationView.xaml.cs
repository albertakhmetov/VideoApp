/*  Copyright � 2025, Albert Akhmetov <akhmetov@live.com>   
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
namespace VideoApp.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VideoApp.Core.ViewModels;

public sealed partial class NotificationView : UserControl
{
    public static DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(PlayerViewModel),
        typeof(NotificationView),
        new PropertyMetadata(null, OnViewModelChanged));

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NotificationView control)
        {
            if (e.NewValue is PlayerViewModel viewModel)
            {
                control.Bindings?.Update();
            }
            else
            {
                control.Bindings?.StopTracking();
            }
        }
    }

    public NotificationView()
    {
        this.InitializeComponent();

        this.Unloaded += OnUnloaded;
    }

    public PlayerViewModel? ViewModel
    {
        get => (PlayerViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Bindings?.StopTracking();
    }
}
