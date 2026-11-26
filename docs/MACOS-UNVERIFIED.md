# macOS：未验证说明

本机为 Windows 11，无 Mac、无 Xcode、无 Unity macOS 构建/测试环境。macOS 分支仅交付源码与构建脚本，
**未编译、未运行、未录屏**；不能在 PR/报告中声称 macOS 已验收。

## 已交付源码

- `Native~/macOS/PetMac.mm`（Objective-C++，Cocoa + CoreGraphics）
  - NSWindow：`opaque=NO`、clear 背景、无阴影、floating 层级、all-spaces、accessory app
  - 点击穿透：`CGEventGetLocation` 命中宠物矩形时切换 `ignoresMouseEvents`
  - 窗口枚举/跟随：`CGWindowListCopyWindowInfo`，按 `CGWindowID` 保留目标
  - 坐标：CG top-left ↔ AppKit bottom-left 转换；工作区 `[NSScreen visibleFrame]`
- `Native~/macOS/build-macos.sh`：clang++ 构建 `.bundle`
- `Assets/Scripts/Platform/MacPlatform.cs`：C ABI P/Invoke；已接入窗口配置/圆盘命中/动态穿透，
  顶边吸附/跟随/落回仍是 TODO（未验证）

## 需要在 Mac 上补做

1. 安装 Unity 2022.3.62f3c1（或同系列 LTS）+ Xcode Command Line Tools
2. `bash Native~/macOS/build-macos.sh`，产物放 `Assets/Plugins/macOS/PetMac.bundle`
3. 接入 `MacPlatform.cs` 的 C ABI，构建 `.app`
4. 真机验证透明/置顶/动态穿透/不抢焦点 + 顶边吸附/跟随/落回
5. 录 30–45 秒视频，更新 `TEST_MATRIX.md`

## 最大风险

普通 borderless `NSWindow` 点击仍可能激活 App；`NSWindowStyleMaskNonactivatingPanel` 只对 `NSPanel` 有效。
需要在真机上用 `object_setClass` 换自定义 NSWindow 子类，或 swizzle `canBecomeKeyWindow` /
`canBecomeMainWindow` 返回 NO。此点未验证。
