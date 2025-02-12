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

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using VideoApp.Core.Services;
using Windows.Win32;
using Windows.Win32.Foundation;

class SingleInstanceService : ISingleInstanceService
{
    public static bool IsFirstInstance(string[] args)
    {
        appInstance = AppInstance.FindOrRegisterForKey("com.albertakhmetov.videoapp");

        if (!appInstance.IsCurrent)
        {
            PipeService.SendData(GetData(args)).Wait();

            AppActivationArguments a = AppInstance.GetCurrent().GetActivatedEventArgs();
            appInstance.RedirectActivationToAsync(a).AsTask().Wait(TimeSpan.FromSeconds(5));

            return false;
        }
        else
        {
            appInstance.Activated += OnAppInstanceActivated;

            return true;
        }
    }

    private static string GetData(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return string.Empty;
        }
        else
        {
            var sb = new StringBuilder();
            foreach (var i in args)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.Append(i);
            }

            return sb.ToString();
        }
    }

    private static void OnAppInstanceActivated(object? sender, AppActivationArguments e)
    {
        if (appInstance != null)
        {
            var process = Process.GetProcessById((int)appInstance!.ProcessId);
            PInvoke.SetForegroundWindow(new HWND(process.MainWindowHandle));
        }
    }

    private static AppInstance? appInstance;

    private Subject<ImmutableArray<string>> activatedSubject;

    public SingleInstanceService()
    {
        activatedSubject = new Subject<ImmutableArray<string>>();
        Activated = activatedSubject.AsObservable();
    }

    public IObservable<ImmutableArray<string>> Activated { get; }

    public void OnActivated(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            activatedSubject.OnNext(ImmutableArray<string>.Empty);
        }
        else
        {
            using var reader = new StringReader(data);

            var builder = ImmutableArray.CreateBuilder<string>();

            var line = default(string);
            while ((line = reader.ReadLine()) != null)
            {
                builder.Add(line);
            }

            activatedSubject.OnNext(builder.ToImmutable());
        }
    }
}
