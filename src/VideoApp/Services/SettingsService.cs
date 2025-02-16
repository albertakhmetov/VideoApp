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
namespace VideoApp.Services;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

internal class SettingsService : ISettingsService, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly Subject<bool> uiSubject;

    private FileInfo settingsFile;
    private readonly BehaviorSubject<bool> isModifiedSubject;

    private readonly BehaviorSubject<AppTheme> themeSubject;
    private readonly BehaviorSubject<bool> remainingTimeSubject;

    public SettingsService()
    {
        settingsFile = new FileInfo("./settings.json");

        uiSubject = new Subject<bool>();

        var poco = Load();

        themeSubject = new BehaviorSubject<AppTheme>(poco.Theme);
        remainingTimeSubject = new BehaviorSubject<bool>(poco.RemainingTime);

        isModifiedSubject = new BehaviorSubject<bool>(false);
        isModifiedSubject
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .Where(x => x)
            .Subscribe(_ => Save())
            .DisposeWith(disposable);

        Observable
            .Merge(
                themeSubject.Select(_ => true).Skip(1),
                remainingTimeSubject.Select(_ => true).Skip(1))
            .Subscribe(isModifiedSubject)
            .DisposeWith(disposable);

        UI = uiSubject.AsObservable();
        Theme = themeSubject.AsObservable();
        RemainingTime = remainingTimeSubject.AsObservable();
    }

    public IObservable<bool> UI { get; }

    public IObservable<AppTheme> Theme { get; }

    public IObservable<bool> RemainingTime { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    public void SetTheme(AppTheme value)
    {
        themeSubject.OnNext(value);
    }

    public void SetRemainingTime(bool value)
    {
        remainingTimeSubject.OnNext(value);
    }

    public void Show()
    {
        uiSubject.OnNext(true);
    }

    public void Hide()
    {
        uiSubject.OnNext(false);
    }

    private void Save()
    {
        var tempFile = new FileInfo(settingsFile.FullName + ".temp");
        using (var stream = tempFile.Create())
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var poco = new POCO
            {
                Theme = themeSubject.Value,
                RemainingTime = remainingTimeSubject.Value
            };

            JsonSerializer.Serialize(stream, poco, options);
        }

        settingsFile.Refresh();
        if (settingsFile.Exists)
        {
            tempFile.Replace(settingsFile.FullName, settingsFile.FullName + ".backup");
        }
        else
        {
            tempFile.MoveTo(settingsFile.FullName);
        }

        isModifiedSubject.OnNext(false);
    }

    private POCO Load()
    {
        settingsFile.Refresh();
        try
        {
            using var stream = settingsFile.OpenRead();
            var options = new JsonSerializerOptions { WriteIndented = true };

            return JsonSerializer.Deserialize<POCO>(stream, options) ?? new POCO();
        }
        catch (Exception)
        {
            return new POCO();
        }
    }

    private sealed class POCO
    {
        public AppTheme Theme { get; set; }

        public bool RemainingTime { get; set; }
    }
}
