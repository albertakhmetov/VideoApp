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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using VideoApp.Core;
using VideoApp.Core.Services;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

class SystemEventsService : ISystemEventsService, IDisposable
{
    private const uint WM_WININICHANGE = 0x001A;

    private CompositeDisposable disposable = new CompositeDisposable();

    private NativeWindow window;
    private readonly BehaviorSubject<bool> darkThemeSubject;

    public SystemEventsService()
    {
        window = new NativeWindow(this).DisposeWith(disposable);

        darkThemeSubject = new BehaviorSubject<bool>(ShouldAppsUseDarkMode());

        DarkTheme = darkThemeSubject.AsObservable();
    }

    public IObservable<bool> DarkTheme { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    private void ProcessMessage(uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_WININICHANGE && Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet")
        {
            darkThemeSubject.OnNext(ShouldAppsUseDarkMode());
            return;
        }
    }

    [DllImport("UxTheme.dll", EntryPoint = "#132", SetLastError = true)]
    static extern bool ShouldAppsUseDarkMode();

    private sealed class NativeWindow : IDisposable
    {
        private readonly string windowId;
        private SystemEventsService owner;

        private WNDPROC proc;

        public unsafe NativeWindow(SystemEventsService owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

            windowId = $"class:com.albertakhmetov.videoapp.events";
            proc = OnWindowMessageReceived;

            fixed (char* className = windowId)
            {
                var classInfo = new WNDCLASSW()
                {
                    lpfnWndProc = proc,
                    lpszClassName = new PCWSTR(className),
                };

                PInvoke.RegisterClass(classInfo);

                Hwnd = PInvoke.CreateWindowEx(
                    dwExStyle: 0,
                    lpClassName: windowId,
                    lpWindowName: windowId,
                    dwStyle: 0,
                    X: 0,
                    Y: 0,
                    nWidth: 0,
                    nHeight: 0,
                    hWndParent: new HWND(IntPtr.Zero),
                    hMenu: null,
                    hInstance: null,
                    lpParam: null);
            }
        }

        ~NativeWindow()
        {
            Dispose(false);
        }

        public HWND Hwnd { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (Hwnd != HWND.Null)
            {
                PInvoke.DestroyWindow(hWnd: Hwnd);
                Hwnd = HWND.Null;

                PInvoke.UnregisterClass(
                    lpClassName: windowId,
                    hInstance: null);
            }
        }

        private LRESULT OnWindowMessageReceived(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            owner.ProcessMessage(msg, wParam, lParam);

            return PInvoke.DefWindowProc(
                hWnd: hwnd,
                Msg: msg,
                wParam: wParam,
                lParam: lParam);
        }
    }
}
