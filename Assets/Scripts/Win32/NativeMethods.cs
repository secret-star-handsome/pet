using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XiaosuPet.Win32
{
    internal static class NativeMethods
    {
        // Window styles
        internal const int GWL_STYLE = -16;
        internal const int GWL_EXSTYLE = -20;
        internal const int GWLP_WNDPROC = -4;

        internal const long WS_POPUP = 0x80000000L;
        internal const long WS_VISIBLE = 0x10000000L;
        internal const long WS_CAPTION = 0x00C00000L;
        internal const long WS_THICKFRAME = 0x00040000L;
        internal const long WS_MINIMIZEBOX = 0x00020000L;
        internal const long WS_MAXIMIZEBOX = 0x00010000L;
        internal const long WS_SYSMENU = 0x00080000L;

        internal const long WS_EX_TOPMOST = 0x00000008L;
        internal const long WS_EX_TOOLWINDOW = 0x00000080L;
        internal const long WS_EX_NOACTIVATE = 0x08000000L;
        internal const long WS_EX_APPWINDOW = 0x00040000L;
        internal const long WS_EX_LAYERED = 0x00080000L;
        internal const long WS_EX_TRANSPARENT = 0x00000020L;

        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_FRAMECHANGED = 0x0020;
        internal const uint SWP_SHOWWINDOW = 0x0040;
        internal const uint SWP_NOOWNERZORDER = 0x0200;

        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        // Window messages
        internal const uint WM_MOUSEACTIVATE = 0x0021;
        internal const uint WM_NCHITTEST = 0x0084;
        internal const uint WM_MOUSEMOVE = 0x0200;
        internal const uint WM_LBUTTONDOWN = 0x0201;
        internal const uint WM_LBUTTONUP = 0x0202;
        internal const uint WM_RBUTTONDOWN = 0x0204;
        internal const uint WM_RBUTTONUP = 0x0205;
        internal const uint WM_HOTKEY = 0x0312;
        internal const uint WM_DPICHANGED = 0x02E0;

        internal const int HTCLIENT = 1;
        internal const int MA_NOACTIVATE = 3;

        internal const int VK_LBUTTON = 0x01;
        internal const int VK_RBUTTON = 0x02;

        internal const uint MOD_ALT = 0x0001;
        internal const uint MOD_CONTROL = 0x0002;
        internal const uint MOD_NOREPEAT = 0x4000;

        internal const int GW_HWNDNEXT = 2;
        internal const uint GA_ROOT = 2;
        internal const uint MONITOR_DEFAULTTONEAREST = 2;

        // DWM attributes
        internal const int DWMWA_NCRENDERING_POLICY = 2;
        internal const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        internal const int DWMWA_CLOAKED = 14;
        internal const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        internal const int DWMWA_BORDER_COLOR = 34;

        internal const int DWMWCP_DONOTROUND = 1;
        internal const int DWMNCRP_DISABLED = 1;
        internal const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        internal static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        internal static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmGetWindowAttribute(
            IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        internal static extern int DwmGetWindowAttributeInt(
            IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateEllipticRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("user32.dll")]
        internal static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        internal static extern IntPtr CallWindowProc(
            IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("shcore.dll")]
        internal static extern int GetProcessDpiAwareness(IntPtr hprocess, out int value);

        [DllImport("user32.dll")]
        internal static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    }
}
