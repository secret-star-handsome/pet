using System;
using UnityEngine;

namespace XiaosuPet.Win32
{
    /// <summary>
    /// Mouse handling without Unity Input/EventSystem. A WS_EX_NOACTIVATE window still
    /// receives mouse messages; SetCapture keeps drags alive outside the disc.
    /// </summary>
    internal static class Win32Input
    {
        private static Win32Window _window;
        private static PetApp _app;
        private static bool _leftDown;
        private static bool _dragging;
        private static bool _dragNotified;
        private static bool _quitRequested;
        private static bool _rightToggle;
        private static NativeMethods.POINT _grabCursor;
        private static NativeMethods.RECT _grabWindow;
        private static int _downTick;

        public static void Initialize(Win32Window window, PetApp app)
        {
            _window = window;
            _app = app;
            _leftDown = false;
            _dragging = false;
            _dragNotified = false;
            _quitRequested = false;
            _rightToggle = false;
        }

        public static void Shutdown()
        {
            NativeMethods.ReleaseCapture();
            _window = null;
            _app = null;
            _leftDown = false;
            _dragging = false;
            _dragNotified = false;
            _quitRequested = false;
            _rightToggle = false;
        }

        public static void RequestQuit()
        {
            _quitRequested = true;
        }

        public static void HandleLeftDown(NativeMethods.POINT clientPoint)
        {
            if (_window == null) return;

            // Use the click message position, not GetCursorPos: SetCursorPos/injected input
            // can be one message behind, which would turn a stationary click into a drag.
            NativeMethods.POINT cursor = clientPoint;
            if (!NativeMethods.ClientToScreen(_window.Hwnd, ref cursor))
            {
                NativeMethods.GetCursorPos(out cursor);
            }

            if (_app != null && _app.IsQuitBadgeHit(cursor))
            {
                _quitRequested = true;
                return;
            }

            NativeMethods.RECT windowRect;
            if (!_window.TryGetWindowRect(out windowRect)) return;

            _leftDown = true;
            _dragging = false;
            _dragNotified = false;
            _grabCursor = cursor;
            _grabWindow = windowRect;
            _downTick = Environment.TickCount;
            NativeMethods.SetCapture(_window.Hwnd);
        }

        public static void HandleLeftUp()
        {
            if (!_leftDown) return;

            _leftDown = false;
            NativeMethods.ReleaseCapture();

            int elapsed = Environment.TickCount - _downTick;
            if (_dragging)
            {
                _dragging = false;
                _dragNotified = false;
                if (_app != null) _app.OnDragEnded();
            }
            else if (elapsed >= 0 && elapsed <= PetConfig.ClickMaxMilliseconds)
            {
                if (_app != null) _app.OnPetClicked();
            }
        }

        public static void HandleMouseMove()
        {
            // Polled in Tick; kept for WndProc symmetry.
        }

        public static void HandleRightDown()
        {
            _rightToggle = true;
        }

        public static void Tick()
        {
            if (_quitRequested)
            {
                _quitRequested = false;
                Application.Quit();
                return;
            }

            if (_rightToggle)
            {
                _rightToggle = false;
                if (_app != null) _app.ToggleQuitBadge();
            }

            if (_window == null || !_window.IsInitialized) return;

            bool leftPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
            if (_leftDown && !leftPressed)
            {
                // WM_LBUTTONUP was lost (capture change / alt-tab). End the drag safely.
                _leftDown = false;
                NativeMethods.ReleaseCapture();
                if (_dragging)
                {
                    _dragging = false;
                    _dragNotified = false;
                    if (_app != null) _app.OnDragEnded();
                }
                return;
            }

            if (!_leftDown) return;

            NativeMethods.POINT cursor;
            if (!NativeMethods.GetCursorPos(out cursor)) return;

            int dx = cursor.X - _grabCursor.X;
            int dy = cursor.Y - _grabCursor.Y;

            if (!_dragging)
            {
                int threshold = Mathf.Max(1, Mathf.RoundToInt(PetConfig.ClickMaxMoveDip * _window.DpiScale));
                if (Math.Abs(dx) + Math.Abs(dy) >= threshold)
                {
                    _dragging = true;
                    if (!_dragNotified)
                    {
                        _dragNotified = true;
                        if (_app != null) _app.OnDragStarted();
                    }
                }
            }

            if (_dragging)
            {
                _window.MoveTo(_grabWindow.Left + dx, _grabWindow.Top + dy);
            }
        }
    }
}
