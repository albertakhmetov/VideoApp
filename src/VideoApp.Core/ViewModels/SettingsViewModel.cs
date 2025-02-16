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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class SettingsViewModel : ViewModel
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly ISettingsService settingsService;

    private AppTheme theme;

    public SettingsViewModel(IApp app, ISettingsService settingsService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.settingsService = settingsService.NotNull();

        Info = app.NotNull().Info;

        this.settingsService
            .Theme
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Theme = x)
            .DisposeWith(disposable);

        CloseCommand = new RelayCommand(_ => this.settingsService.Hide());
    }

    public AppTheme Theme
    {
        get => theme;
        set
        {
            if (Set(ref theme, value))
            {
                settingsService.SetTheme(value);
            }
        }
    }

    public ImmutableArray<AppTheme> Themes { get; } = ImmutableArray.Create(Enum.GetValues<AppTheme>());

    public bool RemainingTime { get; } = true;

    public AppInfo Info { get; }

    public ICommand CloseCommand { get; }
}
