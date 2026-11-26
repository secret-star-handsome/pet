using UnityEngine;

namespace XiaosuPet
{
    /// <summary>Editor / unsupported-platform fallback. Does not touch any OS window.</summary>
    public sealed class NullPlatform : IPlatformWindow
    {
        public void Initialize()
        {
            Debug.Log("[Xiaosu] NullPlatform: transparent/click-through only runs in a standalone build.");
        }

        public void Tick() { }

        public void Shutdown() { }

        public void OnDragStarted() { }

        public void OnDragEnded() { }
    }
}
