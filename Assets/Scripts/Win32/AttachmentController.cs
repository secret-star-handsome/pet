using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XiaosuPet.Win32
{
    /// <summary>
    /// Free / Attached / Falling state machine. On drop, finds a real window top edge,
    /// snaps the visual disc onto it, follows the target every frame, and falls back to
    /// the desktop when the target is minimized, hidden, cloaked, or closed.
    /// </summary>
    internal sealed class AttachmentController
    {
        private enum State
        {
            Free,
            Attached,
            Falling
        }

        private readonly Win32Window _window;
        private readonly WindowTracker _tracker;

        private State _state = State.Free;
        private WindowInfo _target;
        private int _anchorX;
        private int _centerOffsetX;
        private int _bottomOffsetY;
        private int _lastX;
        private int _lastY;
        private bool _hasLastPosition;

        private float _fallTime;
        private int _fallStartX;
        private int _fallStartY;
        private int _fallEndX;
        private int _fallEndY;

        public AttachmentController(Win32Window window, WindowTracker tracker)
        {
            _window = window;
            _tracker = tracker;
        }

        public void OnDragStarted()
        {
            if (_state == State.Attached || _state == State.Falling)
            {
                _state = State.Free;
                _hasLastPosition = false;
                Debug.Log("[Xiaosu] detached by drag");
            }
        }

        public void OnDragEnded()
        {
            if (_state == State.Falling) return;
            TryAttach();
        }

        public void Tick(float deltaTime)
        {
            if (_window == null || !_window.IsInitialized) return;

            if (_state == State.Attached)
            {
                FollowTarget();
            }
            else if (_state == State.Falling)
            {
                TickFall(deltaTime);
            }
        }

        private void TryAttach()
        {
            NativeMethods.RECT windowRect;
            NativeMethods.RECT client;
            if (!_window.TryGetWindowRect(out windowRect) ||
                !_window.TryGetClientRect(out client))
            {
                return;
            }

            var clientOrigin = new NativeMethods.POINT { X = 0, Y = 0 };
            if (!NativeMethods.ClientToScreen(_window.Hwnd, ref clientOrigin)) return;
            if (client.Width <= 0 || client.Height <= 0) return;

            int petCenterX = clientOrigin.X + client.Width / 2;
            int petDiscBottom = clientOrigin.Y + Mathf.RoundToInt(client.Height * 0.75f);
            int tolerance = Mathf.RoundToInt(PetConfig.AttachToleranceDip * _window.DpiScale);

            WindowInfo target;
            int anchorX;
            if (!_tracker.TryFindTopEdgeTarget(
                    new NativeMethods.POINT { X = petCenterX, Y = petDiscBottom },
                    tolerance,
                    out target,
                    out anchorX))
            {
                _state = State.Free;
                Debug.Log("[Xiaosu] drop had no attach target");
                return;
            }

            int deltaX = (target.Rect.Left + anchorX) - petCenterX;
            int deltaY = target.Rect.Top - petDiscBottom;
            _window.MoveTo(windowRect.Left + deltaX, windowRect.Top + deltaY);

            _target = target;
            _anchorX = anchorX;
            _centerOffsetX = petCenterX - windowRect.Left;
            _bottomOffsetY = petDiscBottom - windowRect.Top;
            _hasLastPosition = false;
            _state = State.Attached;

            Debug.Log(string.Format(
                "[Xiaosu] attached to class={0} pid={1} rect=({2},{3})-({4},{5})",
                target.ClassName, target.ProcessId,
                target.Rect.Left, target.Rect.Top, target.Rect.Right, target.Rect.Bottom));
        }

        private void FollowTarget()
        {
            if (!_tracker.IsTargetAlive(_target))
            {
                StartFall("target gone");
                return;
            }

            NativeMethods.RECT targetRect;
            if (!_tracker.TryGetTargetRect(_target, out targetRect))
            {
                StartFall("target rect unavailable");
                return;
            }

            NativeMethods.RECT work;
            if (NativeMethods.IsZoomed(_target.Hwnd) ||
                !_tracker.TryGetWorkArea(_target.Hwnd, out work) ||
                targetRect.Top <= work.Top + 2)
            {
                StartFall("target reached screen top");
                return;
            }

            if (_anchorX < 0) _anchorX = 0;
            if (_anchorX > targetRect.Width) _anchorX = targetRect.Width;

            int x = targetRect.Left + _anchorX - _centerOffsetX;
            int y = targetRect.Top - _bottomOffsetY;

            if (!_hasLastPosition || x != _lastX || y != _lastY)
            {
                _window.MoveTo(x, y);
                _lastX = x;
                _lastY = y;
                _hasLastPosition = true;
            }
        }

        private void StartFall(string reason)
        {
            NativeMethods.RECT windowRect;
            NativeMethods.RECT client;
            if (!_window.TryGetWindowRect(out windowRect) ||
                !_window.TryGetClientRect(out client))
            {
                _state = State.Free;
                return;
            }

            var clientOrigin = new NativeMethods.POINT { X = 0, Y = 0 };
            if (!NativeMethods.ClientToScreen(_window.Hwnd, ref clientOrigin))
            {
                _state = State.Free;
                return;
            }

            NativeMethods.RECT work;
            if (!_tracker.TryGetWorkArea(_window.Hwnd, out work))
            {
                _state = State.Free;
                return;
            }

            int petDiscBottomOffset = (clientOrigin.Y + Mathf.RoundToInt(client.Height * 0.75f)) - windowRect.Top;
            int endX = Mathf.Clamp(windowRect.Left, work.Left, work.Right - windowRect.Width);
            int endY = work.Bottom - petDiscBottomOffset;

            _fallStartX = windowRect.Left;
            _fallStartY = windowRect.Top;
            _fallEndX = endX;
            _fallEndY = endY;
            _fallTime = 0f;
            _hasLastPosition = false;
            _state = State.Falling;

            Debug.Log("[Xiaosu] falling: " + reason);
        }

        private void TickFall(float deltaTime)
        {
            _fallTime += deltaTime;
            float t = Mathf.Clamp01(_fallTime / PetConfig.FallDurationSeconds);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            int x = Mathf.RoundToInt(Mathf.Lerp(_fallStartX, _fallEndX, eased));
            int y = Mathf.RoundToInt(Mathf.Lerp(_fallStartY, _fallEndY, eased));
            _window.MoveTo(x, y);

            if (t >= 1f)
            {
                _state = State.Free;
                _hasLastPosition = false;
                Debug.Log("[Xiaosu] fall complete");
            }
        }
    }
}
