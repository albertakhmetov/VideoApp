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
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class PlayerViewModel : ViewModel, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IServiceProvider serviceProvider;
    private readonly IApp app;
    private readonly IPlaybackService playbackService;

    private int volume;
    private PlaybackState state;

    public PlayerViewModel(IServiceProvider serviceProvider, IApp app, IPlaybackService playbackService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.serviceProvider = serviceProvider.NotNull();
        this.app = app.NotNull();
        this.playbackService = playbackService.NotNull();

        playbackService
            .State
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => State = x)
            .DisposeWith(disposable);

        OpenMediaFileCommand = this.serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(OpenMediaFileCommand));


        SettingsCommand = this.serviceProvider
            .GetRequiredKeyedService<CommandBase>(nameof(SettingsCommand));

        ToggleFullScreenCommand = new RelayCommand(x => this.app.SetFullScreenMode(x is bool isEnabled ? isEnabled : null));
        
        ExitCommand = new RelayCommand(_ => app.Exit());

        MruListViewModel = serviceProvider.GetRequiredService<MruListViewModel>();
        PlaylistViewModel = serviceProvider.GetRequiredService<PlaylistViewModel>();
        TracksViewModel = serviceProvider.GetRequiredService<TracksViewModel>();
        PlaybackViewModel = serviceProvider.GetRequiredService<PlaybackViewModel>();
        StartupViewModel = serviceProvider.GetRequiredService<StartupViewModel>();

        Settings = serviceProvider.GetRequiredService<SettingsViewModel>();
    }

    public PlaybackState State
    {
        get => state;
        private set => Set(ref state, value);
    }

    public CommandBase OpenMediaFileCommand { get; }

    public ICommand SettingsCommand { get; }

    public ICommand ToggleFullScreenCommand { get; }

    public ICommand ExitCommand { get; }

    public MruListViewModel MruListViewModel { get; }

    public PlaylistViewModel PlaylistViewModel { get; }

    public TracksViewModel TracksViewModel { get; }

    public PlaybackViewModel PlaybackViewModel { get; }

    public StartupViewModel StartupViewModel { get; }

    public SettingsViewModel Settings { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }
}
