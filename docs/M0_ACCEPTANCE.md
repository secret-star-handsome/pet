# M0 验收对照

> 状态随测试更新；最终以 PR 描述 + 录屏 + `TEST_MATRIX.md` 为准。

## 1. 双平台透明、置顶、动态点击穿透、不抢焦点

- Windows 实现：
  - 透明：`DwmExtendFrameIntoClientArea(-1)` + 相机 clear alpha 0 + `PetUnlitAlpha`
  - 置顶：`SetWindowPos(HWND_TOPMOST, SWP_NOACTIVATE)`
  - 点击穿透：`SetWindowRgn` 圆形区域；区域外由系统路由到下层进程
  - 不抢焦点：`WS_EX_NOACTIVATE` + `WM_MOUSEACTIVATE -> MA_NOACTIVATE`
  - 拖拽：`SetCapture` + `GetCursorPos` + `SetWindowPos`，不依赖 Unity Input
- Windows 证据（已实测）：
  - 全屏截图可见桌面透出；无边框/任务栏项；始终置顶
  - 圆盘外 `WindowFromPoint` 返回下层窗口，点击不触发宠物
  - 点击/拖拽宠物前后 `GetForegroundWindow` 不变；日志 `pet clicked`
  - 详见 `docs/TEST_MATRIX.md` 1–5、17
- macOS：仅 `Native~/macOS/PetMac.mm` 源码 + 构建脚本；**未编译、未实测**，
  `MacPlatform.cs` 只接入窗口配置/穿透，吸附跟随仍是 TODO

## 2. 顶边吸附 → 跟随 → 关闭/最小化落回

- 实现：`Assets/Scripts/Win32/WindowTracker.cs` + `AttachmentController.cs`
- 证据（已实测）：
  - 日志 `attached to class=Notepad pid=... rect=...`
  - 记事本 +100/+50，宠物窗口同步 +100/+50
  - 横向 800→600，宠物保持顶边
  - 最小化/关闭：`falling: target gone` → `fall complete`
  - 落回后点击：`pet clicked`，前台不变

## 3. 高分屏 DPI / 双屏 / 安全退出

- 125%：已测（当前环境）
- 150% / 200% / 100%：需手动切换 Windows 缩放后重启复测（待用户配合）
- 双屏：本机单屏，阻塞
- 安全退出：`Ctrl+Alt+Q` 已实测（进程退出、无 crash handler）；右键 X 徽标暂未接入

## 4. 双平台录屏

- Windows：按 `docs/DEMO_SCRIPT.md` 录制；视频作为 PR 附件/Release 资产，不直接提交仓库（待用户录制）
- macOS：无机器，阻塞
