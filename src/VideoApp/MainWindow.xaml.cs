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
using VideoApp.Core.Services;
using VideoApp.Core.ViewModels;

using Windows.Win32.Foundation;

public partial class MainWindow : Window
{
    private readonly CompositeDisposable disposable = new CompositeDisposable();

    private readonly ISettingsService settingsService;

    public MainWindow(IServiceProvider serviceProvider, ISettingsService settingsService)
    {
        this.settingsService = settingsService.NotNull();
        this.InitializeComponent();

        Content = serviceProvider.NotNull().GetRequiredKeyedService<UserControl>(nameof(PlayerViewModel));

        this.settingsService
            .Theme
            .Subscribe(x => UpdateTheme(x == Core.Models.AppTheme.Dark))
            .DisposeWith(disposable);

        this.Closed += (_, _) => disposable.Dispose();
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
