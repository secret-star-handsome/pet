using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XiaosuPet.Win32
{
    /// <summary>
    /// Turns the Unity player window into a topmost, non-activating, transparent disc.
    /// The circular window region (SetWindowRgn) is what makes clicks outside the disc
    /// pass through to other processes; WM_NCHITTEST/HTTRANSPARENT cannot do that.
    /// </summary>
    internal sealed class Win32Window
    {
        private IntPtr _hwnd;
        private IntPtr _oldWndProc;
        private NativeMethods.WndProcDelegate _wndProc;
        private bool _initialized;
        private int _awareness;
        private uint _dpi = 96;
        private int _regionWidth;
        private int _regionHeight;
        private int _lastRegionTick;

        public IntPtr Hwnd { get { return _hwnd; } }
        public bool IsInitialized { get { return _initialized && _hwnd != IntPtr.Zero; } }
        public float DpiScale { get { return _dpi <= 0 ? 1f : _dpi / 96f; } }
        public int Awareness { get { return _awareness; } }

        public void Initialize()
        {
            _hwnd = FindOwnWindow();
            if (_hwnd == IntPtr.Zero)
            {
                Debug.LogError("[Xiaosu] Could not find the Unity player window.");
                return;
            }

            NativeMethods.GetProcessDpiAwareness(IntPtr.Zero, out _awareness);
            _dpi = NativeMethods.GetDpiForWindow(_hwnd);
            if (_dpi == 0) _dpi = 96;

            ApplyStyles();
            ApplyDwm();
            StabilizeClientSize();
            ApplyRegion();
            SubclassWindow();
            RegisterQuitHotkey();

            PositionBottomRight();
            SetWindowPosToTopmost();

            _initialized = true;
            LogDiagnostics();
        }

        public void Shutdown()
        {
            if (_hwnd == IntPtr.Zero) return;

            NativeMethods.UnregisterHotKey(_hwnd, PetConfig.QuitHotkeyId);

            if (_oldWndProc != IntPtr.Zero)
            {
                NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWLP_WNDPROC, _oldWndProc);
                _oldWndProc = IntPtr.Zero;
            }

            GC.KeepAlive(_wndProc);
            _wndProc = null;
            _initialized = false;
            _hwnd = IntPtr.Zero;
        }

        public void MoveTo(int x, int y)
        {
            if (_hwnd == IntPtr.Zero) return;
            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_TOPMOST,
                x,
                y,
                0,
                0,
                NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE |
                NativeMethods.SWP_NOOWNERZORDER);
        }

        public bool TryGetWindowRect(out NativeMethods.RECT rect)
        {
            rect = default(NativeMethods.RECT);
            return _hwnd != IntPtr.Zero && NativeMethods.GetWindowRect(_hwnd, out rect);
        }

        public bool TryGetClientRect(out NativeMethods.RECT rect)
        {
            rect = default(NativeMethods.RECT);
            return _hwnd != IntPtr.Zero && NativeMethods.GetClientRect(_hwnd, out rect);
        }

        public void ReapplyRegion()
        {
            ApplyRegion();
        }

        private static IntPtr FindOwnWindow()
        {
            uint processId = (uint)Process.GetCurrentProcess().Id;
            IntPtr hwnd = NativeMethods.GetTopWindow(IntPtr.Zero);

            while (hwnd != IntPtr.Zero)
            {
                uint windowProcessId;
                NativeMethods.GetWindowThreadProcessId(hwnd, out windowProcessId);
                if (windowProcessId == processId && NativeMethods.IsWindowVisible(hwnd))
                {
                    var className = new StringBuilder(256);
                    NativeMethods.GetClassName(hwnd, className, className.Capacity);
                    if (className.ToString() == "UnityWndClass")
                    {
                        return hwnd;
                    }
                }

                hwnd = NativeMethods.GetWindow(hwnd, NativeMethods.GW_HWNDNEXT);
            }

            return NativeMethods.GetActiveWindow();
        }

        private void ApplyStyles()
        {
            long style = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE).ToInt64();
            style &= ~(NativeMethods.WS_CAPTION |
                       NativeMethods.WS_THICKFRAME |
                       NativeMethods.WS_MINIMIZEBOX |
                       NativeMethods.WS_MAXIMIZEBOX |
                       NativeMethods.WS_SYSMENU);
            style |= NativeMethods.WS_POPUP | NativeMethods.WS_VISIBLE;
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE, new IntPtr(style));

            long exStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            exStyle |= NativeMethods.WS_EX_TOPMOST |
                       NativeMethods.WS_EX_TOOLWINDOW |
                       NativeMethods.WS_EX_NOACTIVATE;
            exStyle &= ~NativeMethods.WS_EX_APPWINDOW;
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE |
                NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_FRAMECHANGED |
                NativeMethods.SWP_NOACTIVATE |
                NativeMethods.SWP_SHOWWINDOW |
                NativeMethods.SWP_NOOWNERZORDER);
        }

        private void ApplyDwm()
        {
            var margins = new NativeMethods.MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };
            NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

            int corner = NativeMethods.DWMWCP_DONOTROUND;
            NativeMethods.DwmSetWindowAttribute(
                _hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, 4);

            int border = NativeMethods.DWMWA_COLOR_NONE;
            NativeMethods.DwmSetWindowAttribute(
                _hwnd, NativeMethods.DWMWA_BORDER_COLOR, ref border, 4);

            // Do not set DWMWA_NCRENDERING_POLICY here: disabling non-client rendering
            // can conflict with DwmExtendFrameIntoClientArea (black window). Add it only
            // if a visible DWM shadow remains after testing.
        }

        private void StabilizeClientSize()
        {
            // Unity persists the player window size between launches; without this the
            // outer frame grows by a few pixels each run. Force 200 DIP in physical pixels.
            // Do NOT call Screen.SetResolution here: Unity restores its framed window style
            // and drops the region when a resolution change is applied.
            int target = Mathf.RoundToInt(PetConfig.WindowSizeDip * DpiScale);
            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, target, target,
                NativeMethods.SWP_NOMOVE |
                NativeMethods.SWP_NOACTIVATE |
                NativeMethods.SWP_FRAMECHANGED |
                NativeMethods.SWP_NOOWNERZORDER);
        }

        private void ApplyRegion()
        {
            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(_hwnd, out client)) return;

            int width = client.Width;
            int height = client.Height;
            if (width <= 0 || height <= 0) return;

            // The visual disc is half the client height, centered. Convert the client
            // origin into window coordinates so the region matches even with DWM borders.
            int cx = width / 2;
            int cy = height / 2;
            NativeMethods.RECT window;
            if (NativeMethods.GetWindowRect(_hwnd, out window))
            {
                var origin = new NativeMethods.POINT { X = 0, Y = 0 };
                if (NativeMethods.ClientToScreen(_hwnd, ref origin))
                {
                    cx = (origin.X - window.Left) + width / 2;
                    cy = (origin.Y - window.Top) + height / 2;
                }
            }

            int radius = Mathf.RoundToInt(height * 0.25f) + 2;
            int maxRadius = Mathf.Min(width, height) / 2;
            if (radius > maxRadius) radius = maxRadius;

            IntPtr region = NativeMethods.CreateEllipticRgn(
                cx - radius, cy - radius, cx + radius, cy + radius);
            if (region == IntPtr.Zero)
            {
                Debug.LogError("[Xiaosu] CreateEllipticRgn failed.");
                return;
            }

            // On success the system owns the region; do not DeleteObject it.
            NativeMethods.SetWindowRgn(_hwnd, region, true);
            _regionWidth = width;
            _regionHeight = height;
        }

        public void EnsureRegion()
        {
            if (_hwnd == IntPtr.Zero) return;

            // Watchdog: Unity may restore its framed window style on a resolution/focus
            // change. Re-apply our borderless/non-activating styles if that happens.
            long style = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE).ToInt64();
            long exStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            long requiredEx = NativeMethods.WS_EX_NOACTIVATE |
                              NativeMethods.WS_EX_TOOLWINDOW |
                              NativeMethods.WS_EX_TOPMOST;
            bool styleLost = (style & (NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME)) != 0 ||
                             (exStyle & requiredEx) != requiredEx;
            if (styleLost)
            {
                ApplyStyles();
                ApplyDwm();
            }

            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(_hwnd, out client)) return;

            int now = System.Environment.TickCount;
            bool regionCheck = _lastRegionTick == 0 || now - _lastRegionTick >= 1000;
            if (regionCheck) _lastRegionTick = now;

            if (styleLost ||
                regionCheck ||
                client.Width != _regionWidth ||
                client.Height != _regionHeight)
            {
                ApplyRegion();
            }
        }

        private void SubclassWindow()
        {
            _wndProc = WindowProc;
            IntPtr newProc = Marshal.GetFunctionPointerForDelegate(_wndProc);
            _oldWndProc = NativeMethods.SetWindowLongPtr(
                _hwnd, NativeMethods.GWLP_WNDPROC, newProc);

            if (_oldWndProc == IntPtr.Zero)
            {
                Debug.LogError("[Xiaosu] SetWindowLongPtr(GWLP_WNDPROC) failed: " +
                               Marshal.GetLastWin32Error());
            }
        }

        private void RegisterQuitHotkey()
        {
            bool ok = NativeMethods.RegisterHotKey(
                _hwnd,
                PetConfig.QuitHotkeyId,
                PetConfig.QuitHotkeyModifiers,
                PetConfig.QuitHotkeyVirtualKey);
            if (!ok)
            {
                Debug.LogWarning("[Xiaosu] RegisterHotKey(Ctrl+Alt+Q) failed: " +
                                 Marshal.GetLastWin32Error());
            }
        }

        private void PositionBottomRight()
        {
            NativeMethods.RECT window;
            if (!NativeMethods.GetWindowRect(_hwnd, out window)) return;

            IntPtr monitor = NativeMethods.MonitorFromWindow(
                _hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            var info = new NativeMethods.MONITORINFO
            {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO))
            };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return;

            int margin = Mathf.RoundToInt(40f * DpiScale);
            int x = info.rcWork.Right - window.Width - margin;
            int y = info.rcWork.Bottom - window.Height - margin;

            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_TOPMOST,
                x, y, 0, 0,
                NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE |
                NativeMethods.SWP_NOOWNERZORDER);
        }

        private void SetWindowPosToTopmost()
        {
            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE |
                NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE |
                NativeMethods.SWP_NOOWNERZORDER);
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case NativeMethods.WM_MOUSEACTIVATE:
                    // Never activate/focus the pet window when clicked.
                    return (IntPtr)NativeMethods.MA_NOACTIVATE;

                case NativeMethods.WM_NCHITTEST:
                    // The circular window region already rejects points outside the disc.
                    return (IntPtr)NativeMethods.HTCLIENT;

                case NativeMethods.WM_LBUTTONDOWN:
                    long packed = lParam.ToInt64();
                    int clientX = (short)(packed & 0xFFFF);
                    int clientY = (short)((packed >> 16) & 0xFFFF);
                    Win32Input.HandleLeftDown(new NativeMethods.POINT { X = clientX, Y = clientY });
                    break;

                case NativeMethods.WM_LBUTTONUP:
                    Win32Input.HandleLeftUp();
                    break;

                case NativeMethods.WM_MOUSEMOVE:
                    Win32Input.HandleMouseMove();
                    break;

                case NativeMethods.WM_RBUTTONDOWN:
                    Win32Input.HandleRightDown();
                    break;

                case NativeMethods.WM_HOTKEY:
                    if (wParam.ToInt32() == PetConfig.QuitHotkeyId)
                    {
                        Win32Input.RequestQuit();
                        return IntPtr.Zero;
                    }
                    break;
            }

            if (_oldWndProc == IntPtr.Zero) return IntPtr.Zero;
            return NativeMethods.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void LogDiagnostics()
        {
            NativeMethods.RECT window;
            NativeMethods.GetWindowRect(_hwnd, out window);
            long style = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE).ToInt64();
            long exStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();

            Debug.Log(string.Format(
                "[Xiaosu] hwnd=0x{0:X} awareness={1} dpi={2} rect=({3},{4},{5},{6}) style=0x{7:X} ex=0x{8:X}",
                _hwnd.ToInt64(), _awareness, _dpi,
                window.Left, window.Top, window.Width, window.Height,
                style, exStyle));
        }
    }
}
