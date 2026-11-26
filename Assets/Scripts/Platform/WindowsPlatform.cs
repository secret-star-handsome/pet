using UnityEngine;
using XiaosuPet.Win32;

namespace XiaosuPet
{
    public sealed class WindowsPlatform : IPlatformWindow
    {
        private Win32Window _window;
        private WindowTracker _tracker;
        private AttachmentController _attachment;

        public void Initialize()
        {
            _window = new Win32Window();
            _window.Initialize();

            _tracker = new WindowTracker(_window.Hwnd, _window.Awareness);
            _attachment = new AttachmentController(_window, _tracker);

            Win32Input.Initialize(_window, PetApp.Instance);
        }

        public void Tick()
        {
            if (_window != null) _window.EnsureRegion();
            Win32Input.Tick();
            if (_attachment != null)
            {
                _attachment.Tick(Time.unscaledDeltaTime);
            }
        }

        public void Shutdown()
        {
            Win32Input.Shutdown();
            _attachment = null;
            _tracker = null;

            if (_window != null)
            {
                _window.Shutdown();
                _window = null;
            }
        }

        public void OnDragStarted()
        {
            if (_attachment != null) _attachment.OnDragStarted();
        }

        public void OnDragEnded()
        {
            if (_attachment != null) _attachment.OnDragEnded();
        }
    }
}
