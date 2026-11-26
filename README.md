# 晓苏 · 桌面宠物（M0）

Windows 透明桌宠原型：完全透明背景、始终置顶、点宠物可互动、点透明空白可穿透到下层软件、
点击/拖拽不抢前台输入焦点；把宠物拖到真实应用窗口顶边后可跟随窗口移动，窗口关闭/最小化时落回桌面。

## 技术栈

- **Unity 2022.3.62f3c1 LTS**（中国 CDN 可用；Unity 6 在中国大陆已下架）
- **Built-in Render Pipeline**，仅 D3D11，Gamma，HDR/MSAA/后处理关闭
- **零第三方运行时依赖**：Windows 侧全部通过 C# P/Invoke 调 user32/dwmapi/gdi32
- 透明窗口：`DwmExtendFrameIntoClientArea` + `SetWindowRgn` 圆形窗口区域
- 不抢焦点：`WS_EX_NOACTIVATE` + `WM_MOUSEACTIVATE -> MA_NOACTIVATE`
- 点击穿透：圆形 region 之外的像素不属于窗口，系统直接路由到下层进程
  （`WM_NCHITTEST -> HTTRANSPARENT` 只对同线程窗口生效，不能跨进程，故未采用）

## 构建

前置：Unity 2022.3.62f3c1，Personal 许可证已激活；Windows Mono 构建支持随编辑器自带。

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"

& $unity -batchmode -quit -projectPath "E:\xiaosu-pet" `
  -executeMethod XiaosuPet.Editor.ProjectBootstrap.Run `
  -logFile "E:\xiaosu-pet\Logs\bootstrap.log"

& $unity -batchmode -quit -projectPath "E:\xiaosu-pet" -buildTarget Win64 `
  -executeMethod XiaosuPet.Editor.BuildPipelineRunner.BuildWindows `
  -logFile "E:\xiaosu-pet\Logs\build-win.log"
```

产物：`Build\Windows\xiaosu-pet.exe`。直接运行 exe，不要在 Unity Editor 里验证透明/穿透。

## 操作

- 左键拖拽：移动宠物；从已吸附状态拖走会先脱离
- 左键单击：颜色闪烁 + 小跳，且不抢前台输入焦点
- 拖到真实窗口顶边松手：吸附并跟随；目标最小化/关闭时平滑落回桌面
- `Ctrl+Alt+Q`：全局退出（无焦点也可用）
- 右键退出徽标：暂未接入（使用 `Ctrl+Alt+Q`）

## 已验证（Windows 11 26200 / 125% / 2022.3.62f3c1）

- 完全透明、置顶、无任务栏/Alt-Tab 项
- 圆盘外点击穿透到下层窗口；圆盘内可点击/拖拽
- 点击/拖拽宠物不改变前台窗口（`GetForegroundWindow` 实测）
- Notepad 顶边吸附、窗口移动 +100/+50 同步跟随、横向缩放保持顶边
- 最小化/关闭 Notepad 平滑落回，落回后点击仍有反馈
- `Ctrl+Alt+Q` 退出，无残留进程

## 当前限制（M0）

- macOS 仅交付 `Native~/macOS` 源码与构建脚本，本机无 Mac/Xcode，未编译、未实测；C# 侧吸附/跟随仍是 TODO。
- 本机只有单显示器，跨屏/混合 DPI 未实测；150%/200% 缩放需手动切换后复测。
- Unity Personal 的启动 Splash 无法关闭。
- 独占全屏游戏、最大化窗口顶边、cloaked/UWP 特殊窗口暂不支持，详见 `docs/TEST_MATRIX.md`。

## 文档

- `docs/M0_ACCEPTANCE.md`：任务卡逐项证据
- `docs/TEST_MATRIX.md`：真机测试记录
- `docs/DEMO_SCRIPT.md`：30–45 秒录屏脚本
- `docs/WINDOWS-NOTES.md`：Win32 方案与已知坑
- `docs/MACOS-UNVERIFIED.md`：macOS 未验证说明
