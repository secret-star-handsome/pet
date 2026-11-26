using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace XiaosuPet
{
    /// <summary>
    /// macOS backend. The Objective-C++ plugin lives in Native~/macOS and is built on a Mac.
    /// This file only wires window setup and disc hit-testing; follow/attach is still TODO
    /// and the whole branch is UNVERIFIED (no Mac in this environment).
    /// </summary>
    public sealed class MacPlatform : IPlatformWindow
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential)]
        private struct PetRect
        {
            public double x;
            public double y;
            public double width;
            public double height;
        }

        [DllImport("PetMac", CallingConvention = CallingConvention.Cdecl)]
        private static extern int petmac_setup();

        [DllImport("PetMac", CallingConvention = CallingConvention.Cdecl)]
        private static extern void petmac_set_interactive(int interactive);

        [DllImport("PetMac", CallingConvention = CallingConvention.Cdecl)]
        private static extern void petmac_get_cursor(out double x, out double y);

        [DllImport("PetMac", CallingConvention = CallingConvention.Cdecl)]
        private static extern int petmac_get_own_frame(out PetRect rect);

        [DllImport("PetMac", CallingConvention = CallingConvention.Cdecl)]
        private static extern int petmac_get_work_area(out PetRect rect);

        [DllImport("PetMac", CallingConvention = CallingConvention.Cdecl)]
        private static extern void petmac_set_frame(
            double x, double y, double width, double height);

        private bool _ready;

        public void Initialize()
        {
            try
            {
                if (petmac_setup() == 0)
                {
                    Debug.LogError("[Xiaosu] petmac_setup failed.");
                    return;
                }

                PetRect work;
                if (petmac_get_work_area(out work))
                {
                    double size = PetConfig.WindowSizeDip;
                    double margin = 40.0;
                    double x = work.x + work.width - size - margin;
                    double y = work.y + work.height - size - margin;
                    petmac_set_frame(x, y, size, size);
                }

                _ready = true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[Xiaosu] PetMac load failed: " + exception.Message);
            }
        }

        public void Tick()
        {
            if (!_ready) return;

            double cursorX;
            double cursorY;
            petmac_get_cursor(out cursorX, out cursorY);

            PetRect own;
            if (petmac_get_own_frame(out own) == 0) return;

            double centerX = own.x + own.width * 0.5;
            double centerY = own.y + own.height * 0.5;
            double radius = own.height * 0.25;
            double dx = cursorX - centerX;
            double dy = cursorY - centerY;

            petmac_set_interactive(dx * dx + dy * dy <= radius * radius ? 1 : 0);
        }

        public void Shutdown()
        {
            _ready = false;
        }

        public void OnDragStarted() { }

        public void OnDragEnded() { }
#else
        public void Initialize()
        {
            Debug.LogWarning("[Xiaosu] MacPlatform is only available in a macOS build.");
        }

        public void Tick() { }

        public void Shutdown() { }

        public void OnDragStarted() { }

        public void OnDragEnded() { }
#endif
    }
}
