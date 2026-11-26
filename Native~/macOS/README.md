# macOS 原生插件（未验证）

本目录是 macOS 窗口后端源码，**未在 Mac 上编译/运行/录屏**。Windows 上无法构建或验证。

## 构建

需要 macOS + Xcode Command Line Tools + Unity 2022.3.62f3c1（或同系列 LTS）：

```bash
bash Native~/macOS/build-macos.sh
```

脚本会生成 `PetMac.bundle` 并复制到 `Assets/Plugins/macOS/PetMac.bundle`。

## 已实现（源码级）

- `NSWindow`：`opaque=NO`、clear 背景、无阴影、floating 层级、all-spaces、accessory app
- 非激活：swizzle `canBecomeKeyWindow` / `canBecomeMainWindow` 返回 NO
- 动态点击穿透：按光标是否在宠物圆盘内切换 `ignoresMouseEvents`
- 窗口枚举/跟随基础：`CGWindowListCopyWindowInfo`、按 `CGWindowID` 查询 bounds/alive
- 坐标：CG top-left ↔ AppKit bottom-left；工作区 `[NSScreen visibleFrame]`

## 未实现 / 待验证

- C# `MacPlatform.cs` 只接入了窗口配置、点击穿透和圆盘命中；顶边吸附/跟随/落回状态机仍是 TODO。
- 非激活 swizzle 是最大风险，需真机确认点击不激活 App。
- 需要真机录 30–45 秒视频并更新 `docs/TEST_MATRIX.md`。
- `Assets/Plugins/macOS/*.bundle` 不提交（未验证二进制）；源码与构建脚本提交。
