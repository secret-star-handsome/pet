using UnityEngine;

namespace XiaosuPet
{
    public static class PetConfig
    {
        public const int WindowSizeDip = 200;
        public const float DiscDiameterDip = 100f;
        public const float AttachToleranceDip = 24f;
        public const float ClickMaxMoveDip = 4f;
        public const int ClickMaxMilliseconds = 300;
        public const float FallDurationSeconds = 0.42f;
        public const int TargetFrameRate = 60;
        public const int QuitHotkeyId = 0x5853; // 'XS'
        public const uint QuitHotkeyModifiers = 0x0002 | 0x0001 | 0x4000; // Ctrl + Alt + NoRepeat
        public const uint QuitHotkeyVirtualKey = 0x51; // Q

        public static readonly Color PetColor = new Color32(255, 122, 26, 255);
        public static readonly Color PetFlashColor = new Color32(255, 205, 120, 255);
        public static readonly Color BadgeColor = new Color32(30, 30, 30, 230);
    }
}
