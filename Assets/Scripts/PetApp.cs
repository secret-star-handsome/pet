using System;
using System.Collections;
using UnityEngine;
using XiaosuPet.Win32;

namespace XiaosuPet
{
    public sealed class PetApp : MonoBehaviour
    {
        public static PetApp Instance { get; private set; }

        private Camera _camera;
        private PetVisual _visual;
        private IPlatformWindow _platform;

        private void Awake()
        {
            Instance = this;

            Application.runInBackground = true;
            Application.targetFrameRate = PetConfig.TargetFrameRate;
            QualitySettings.vSyncCount = 0;

            CreateCamera();
            _visual = gameObject.AddComponent<PetVisual>();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            _platform = new WindowsPlatform();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            _platform = new MacPlatform();
#else
            _platform = new NullPlatform();
#endif
        }

        private void Start()
        {
            StartCoroutine(InitializePlatformAfterFirstFrame());
        }

        private IEnumerator InitializePlatformAfterFirstFrame()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // Apply Unity's resolution before we touch the window style. Calling
            // Screen.SetResolution later makes Unity restore a framed window.
            ApplyStartupResolution();
            yield return null;
            yield return null;
#else
            // Let the player window finish its first frame / splash transition.
            yield return null;
#endif
            _platform.Initialize();
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static void ApplyStartupResolution()
        {
            uint dpi = 96;
            IntPtr hwnd = NativeMethods.GetActiveWindow();
            if (hwnd != IntPtr.Zero)
            {
                uint windowDpi = NativeMethods.GetDpiForWindow(hwnd);
                if (windowDpi > 0) dpi = windowDpi;
            }

            int pixels = Mathf.RoundToInt(PetConfig.WindowSizeDip * dpi / 96f);
            Screen.SetResolution(pixels, pixels, FullScreenMode.Windowed);
        }
#endif

        private void Update()
        {
            if (_platform != null) _platform.Tick();
            if (_visual != null) _visual.Tick(Time.unscaledDeltaTime);
        }

        private void OnApplicationQuit()
        {
            if (_platform != null)
            {
                _platform.Shutdown();
                _platform = null;
            }
        }

        public void OnPetClicked()
        {
            if (_visual != null) _visual.Flash();
            Debug.Log("[Xiaosu] pet clicked");
        }

        public void OnDragStarted()
        {
            if (_platform != null) _platform.OnDragStarted();
        }

        public void OnDragEnded()
        {
            if (_platform != null) _platform.OnDragEnded();
        }

        public void ToggleQuitBadge()
        {
            Debug.Log("[Xiaosu] right-click received (quit badge not implemented yet)");
        }

        internal bool IsQuitBadgeHit(NativeMethods.POINT screenPoint)
        {
            // Implemented together with the in-pet X badge in the next stage.
            return false;
        }

        private void CreateCamera()
        {
            var go = new GameObject("PetCamera");
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 0.5f; // window height = 1 world unit
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 10f;
            go.transform.position = new Vector3(0f, 0f, -1f);
            go.tag = "MainCamera";
        }
    }
}
