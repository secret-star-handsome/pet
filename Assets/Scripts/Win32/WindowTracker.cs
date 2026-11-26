using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XiaosuPet.Win32
{
    /// <summary>
    /// Enumerates real top-level application windows, validates candidates, and finds the
    /// window whose top edge the pet was dropped on. All coordinates are in the same space
    /// as the pet (physical pixels on a PerMonitorV2 player).
    /// </summary>
    internal sealed class WindowTracker
    {
        private const int MinTargetWidthDip = 120;
        private const int MinTargetHeightDip = 60;

        private static readonly HashSet<string> ShellClasses = new HashSet<string>(StringComparer.Ordinal)
        {
            "Progman",
            "WorkerW",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "MultitaskingViewFrame",
            "XamlExplorerHostIslandWindow",
            "Windows.UI.Core.CoreWindow"
        };

        private readonly IntPtr _ownHwnd;
        private readonly uint _ownPid;
        private readonly int _awareness;

        public WindowTracker(IntPtr ownHwnd, int awareness)
        {
            _ownHwnd = ownHwnd;
            _awareness = awareness;
            _ownPid = (uint)Process.GetCurrentProcess().Id;
        }

        public bool TryGetWindow(IntPtr hwnd, out WindowInfo info)
        {
            info = default(WindowInfo);
            if (hwnd == IntPtr.Zero || hwnd == _ownHwnd) return false;
            if (!NativeMethods.IsWindow(hwnd)) return false;

            uint processId;
            NativeMethods.GetWindowThreadProcessId(hwnd, out processId);
            if (processId == _ownPid) return false;

            string className = GetClassName(hwnd);
            if (string.IsNullOrEmpty(className) || ShellClasses.Contains(className)) return false;

            long exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0) return false;

            NativeMethods.RECT rect;
            if (!TryGetBounds(hwnd, out rect)) return false;
            if (rect.Width <= 0 || rect.Height <= 0) return false;

            float scale = GetScale(hwnd);
            int minWidth = Mathf.RoundToInt(MinTargetWidthDip * scale);
            int minHeight = Mathf.RoundToInt(MinTargetHeightDip * scale);
            if (rect.Width < minWidth || rect.Height < minHeight) return false;

            info = new WindowInfo
            {
                Hwnd = hwnd,
                ProcessId = processId,
                ClassName = className,
                Rect = rect,
                IsVisible = NativeMethods.IsWindowVisible(hwnd),
                IsMinimized = NativeMethods.IsIconic(hwnd),
                IsCloaked = IsCloaked(hwnd)
            };
            return true;
        }

        public bool TryGetWindowAtPoint(NativeMethods.POINT point, out WindowInfo info)
        {
            info = default(WindowInfo);
            IntPtr hwnd = NativeMethods.WindowFromPoint(point);
            if (hwnd == IntPtr.Zero) return false;
            hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
            return TryGetWindow(hwnd, out info);
        }

        /// <summary>
        /// Finds a valid visible window whose top edge is within tolerance of the pet's
        /// visual bottom, and which is actually the topmost window at that edge (not hidden
        /// behind a foreground window).
        /// </summary>
        public bool TryFindTopEdgeTarget(
            NativeMethods.POINT petDiscBottom,
            int tolerancePixels,
            out WindowInfo target,
            out int anchorX)
        {
            target = default(WindowInfo);
            anchorX = 0;

            bool found = false;
            int bestDistance = int.MaxValue;
            WindowInfo best = default(WindowInfo);

            IntPtr hwnd = NativeMethods.GetTopWindow(IntPtr.Zero);
            while (hwnd != IntPtr.Zero)
            {
                if (hwnd != _ownHwnd)
                {
                    WindowInfo info;
                    if (TryGetWindow(hwnd, out info) &&
                        info.IsVisible &&
                        !info.IsMinimized &&
                        !info.IsCloaked &&
                        petDiscBottom.X >= info.Rect.Left &&
                        petDiscBottom.X < info.Rect.Right)
                    {
                        int distance = Math.Abs(info.Rect.Top - petDiscBottom.Y);
                        if (distance <= tolerancePixels && !NativeMethods.IsZoomed(info.Hwnd))
                        {
                            NativeMethods.RECT work;
                            if (TryGetWorkArea(info.Hwnd, out work) &&
                                info.Rect.Top > work.Top + 2)
                            {
                                int probeY = Math.Max(info.Rect.Top + 4, petDiscBottom.Y + 4);
                                var probe = new NativeMethods.POINT
                                {
                                    X = petDiscBottom.X,
                                    Y = probeY
                                };

                                WindowInfo under;
                                if (TryGetWindowAtPoint(probe, out under) &&
                                    under.Hwnd == info.Hwnd &&
                                    distance < bestDistance)
                                {
                                    best = info;
                                    bestDistance = distance;
                                    found = true;
                                }
                            }
                        }
                    }
                }

                hwnd = NativeMethods.GetWindow(hwnd, NativeMethods.GW_HWNDNEXT);
            }

            if (!found) return false;

            target = best;
            anchorX = petDiscBottom.X - best.Rect.Left;
            return true;
        }

        public bool IsTargetAlive(WindowInfo target)
        {
            if (target.Hwnd == IntPtr.Zero || !NativeMethods.IsWindow(target.Hwnd)) return false;

            uint processId;
            NativeMethods.GetWindowThreadProcessId(target.Hwnd, out processId);
            if (processId != target.ProcessId) return false;
            if (GetClassName(target.Hwnd) != target.ClassName) return false;
            if (!NativeMethods.IsWindowVisible(target.Hwnd)) return false;
            if (NativeMethods.IsIconic(target.Hwnd)) return false;
            if (IsCloaked(target.Hwnd)) return false;
            return true;
        }

        public bool TryGetTargetRect(WindowInfo target, out NativeMethods.RECT rect)
        {
            rect = default(NativeMethods.RECT);
            if (!NativeMethods.IsWindow(target.Hwnd)) return false;
            if (!TryGetBounds(target.Hwnd, out rect)) return false;
            return rect.Width > 0 && rect.Height > 0;
        }

        public bool TryGetWorkArea(IntPtr hwnd, out NativeMethods.RECT work)
        {
            work = default(NativeMethods.RECT);
            IntPtr monitor = NativeMethods.MonitorFromWindow(
                hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return false;

            var info = new NativeMethods.MONITORINFO
            {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO))
            };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return false;

            work = info.rcWork;
            return true;
        }

        private bool TryGetBounds(IntPtr hwnd, out NativeMethods.RECT rect)
        {
            rect = default(NativeMethods.RECT);

            if (_awareness == 2)
            {
                int hr = NativeMethods.DwmGetWindowAttribute(
                    hwnd,
                    NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out rect,
                    Marshal.SizeOf(typeof(NativeMethods.RECT)));
                if (hr == 0 && rect.Width > 0 && rect.Height > 0) return true;
            }

            return NativeMethods.GetWindowRect(hwnd, out rect);
        }

        private static bool IsCloaked(IntPtr hwnd)
        {
            int cloaked;
            int hr = NativeMethods.DwmGetWindowAttributeInt(
                hwnd, NativeMethods.DWMWA_CLOAKED, out cloaked, 4);
            return hr == 0 && cloaked != 0;
        }

        private static string GetClassName(IntPtr hwnd)
        {
            var buffer = new StringBuilder(256);
            NativeMethods.GetClassName(hwnd, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        private static float GetScale(IntPtr hwnd)
        {
            uint dpi = NativeMethods.GetDpiForWindow(hwnd);
            return dpi > 0 ? dpi / 96f : 1f;
        }
    }
}
