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
namespace VideoApp;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.Services;
using VideoApp.Core.ViewModels;

using Windows.Win32.Foundation;

public partial class MainWindow : Window
{
    private readonly CompositeDisposable disposable = new CompositeDisposable();

    private readonly ISystemEventsService systemEventsService;
    private readonly ISettingsService settingsService;
    private readonly IPlaybackService playbackService;
    private readonly UserControl player, settings;

    private bool resumePlaybackAfterSettings;

    public MainWindow(
        IServiceProvider serviceProvider,
        ISystemEventsService systemEventsService,
        ISettingsService settingsService,
        IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.systemEventsService = systemEventsService.NotNull();
        this.settingsService = settingsService.NotNull();
        this.playbackService = playbackService.NotNull();

        this.InitializeComponent();

        player = serviceProvider.NotNull().GetRequiredKeyedService<UserControl>(nameof(PlayerViewModel));
        settings = serviceProvider.NotNull().GetRequiredKeyedService<UserControl>(nameof(SettingsViewModel));
        settings.Visibility = Visibility.Collapsed;

        Root.Children.Add(player);
        Root.Children.Add(settings);

        this.settingsService
            .UI
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ToggleSettins(x))
            .DisposeWith(disposable);

        this.settingsService
            .Theme
            .CombineLatest(this.systemEventsService.DarkTheme.Select(x => x))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateTheme(x.First == AppTheme.System ? x.Second : (x.First == AppTheme.Dark)))
            .DisposeWith(disposable);

        this.Closed += (_, _) => disposable.Dispose();
    }

    private async void ToggleSettins(bool isVisible)
    {
        var state = await playbackService.State.FirstAsync();

        if (isVisible)
        {
            resumePlaybackAfterSettings = state == PlaybackState.Playing;
            if (resumePlaybackAfterSettings)
            {
                playbackService.Pause();
            }

            player.Visibility = Visibility.Collapsed;
            settings.Visibility = Visibility.Visible;
        }
        else
        {
            player.Visibility = Visibility.Visible;
            settings.Visibility = Visibility.Collapsed;

            if (resumePlaybackAfterSettings)
            {
                playbackService.Play();
            }
        }
    }

    private unsafe void UpdateTheme(bool isDarkTheme)
    {
        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));

        var isDark = (uint)(isDarkTheme ? 1 : 0);

        var result = Windows.Win32.PInvoke.DwmSetWindowAttribute(
            hwnd: hwnd,
            dwAttribute: Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            pvAttribute: Unsafe.AsPointer(ref isDark),
            cbAttribute: sizeof(uint));

        if (result != 0)
        {
            throw Marshal.GetExceptionForHR(result) ?? throw new ApplicationException("Can't switch dark mode setting");
        }

        if (Content is FrameworkElement element)
        {
            element.RequestedTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        }
    }
}
