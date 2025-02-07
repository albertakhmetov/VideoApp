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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

internal class SettingsService : ISettingsService
{
    private readonly BehaviorSubject<AppTheme> themeSubject;

    public SettingsService()
    {
        themeSubject = new BehaviorSubject<AppTheme>(AppTheme.Dark);

        Theme = themeSubject.AsObservable();
    }

    public IObservable<AppTheme> Theme { get; }

    public void SetTheme(AppTheme appTheme)
    {
        throw new NotImplementedException();
    }
}
