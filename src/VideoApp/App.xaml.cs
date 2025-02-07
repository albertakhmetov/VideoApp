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

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using VideoApp.Commands;
using VideoApp.Core;
using VideoApp.Core.Commands;
using VideoApp.Core.Services;
using VideoApp.Core.ViewModels;
using VideoApp.Services;
using VideoApp.Views;
using WinRT.Interop;

public partial class App : Application, IApp
{
    private IHost? host;
    private MainWindow? mainWindow;

    private CompositeDisposable disposable = new CompositeDisposable();

    public App()
    {
        InitializeComponent();
    }

    public nint WindowHandle => mainWindow == null ? nint.Zero : WindowNative.GetWindowHandle(mainWindow);

    public void SetFullScreenMode(bool? isEnabled = null)
    {
        if (mainWindow?.AppWindow?.Presenter == null)
        {
            return;
        }

        if (isEnabled == null)
        {
            isEnabled = mainWindow.AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen;
        }

        if (isEnabled == true)
        {
            mainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
        else
        {
            mainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        host = CreateHost();

        mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Closed += OnMainWindowClosed;
        mainWindow.AppWindow.Show(true);

        var view = host.Services.GetRequiredKeyedService<UserControl>(nameof(PlayerViewModel));
        mainWindow.Content = view;

        host.Services.GetRequiredService<IPlaybackService>()
            .MediaFileName
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(SetTitle)
            .DisposeWith(disposable);
    }

    private void SetTitle(string? fileName)
    {
        if(mainWindow == null)
        {
            return;
        }

        var sb = new StringBuilder();

        if (fileName != null)
        {
            sb.Append(Path.GetFileNameWithoutExtension(fileName));
        }

        if (sb.Length > 0)
        {
            sb.Append(" - ");
        }

        sb.Append("VideoApp");

        mainWindow.Title = sb.ToString();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        host?.Dispose();
        Exit();
    }

    private IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<MainWindow>();

        builder.Services.AddSingleton<IApp>(this);
        builder.Services.AddSingleton<IPlaybackService, PlaybackService>();

        builder.Services.AddTransient<PlayerViewModel>();

        builder.Services.AddKeyedSingleton<UserControl, PlayerView>(nameof(PlayerViewModel));

        builder.Services.AddKeyedSingleton<CommandBase, OpenMediaFileCommand>(nameof(OpenMediaFileCommand));

        return builder.Build();
    }
}