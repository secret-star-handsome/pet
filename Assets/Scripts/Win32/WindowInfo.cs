using System;

namespace XiaosuPet.Win32
{
    internal struct WindowInfo
    {
        public IntPtr Hwnd;
        public uint ProcessId;
        public string ClassName;
        public NativeMethods.RECT Rect;
        public bool IsMinimized;
        public bool IsCloaked;
        public bool IsVisible;
    }
}
