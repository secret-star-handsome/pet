# Windows 实现笔记

## 为什么不用 WM_NCHITTEST -> HTTRANSPARENT

`HTTRANSPARENT` 只让鼠标消息在同线程的窗口之间继续传递，**不能跨进程**把点击交给记事本/浏览器。
因此采用 `SetWindowRgn` 把窗口裁成宠物圆盘：区域外的像素在系统层面不属于该窗口，点击自然落到下层。

## 透明配方

1. `WS_POPUP`，去掉 caption/thickframe；`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`
2. `DwmExtendFrameIntoClientArea(hwnd, MARGINS{-1,-1,-1,-1})`
3. Win11：`DWMWA_WINDOW_CORNER_PREFERENCE=DONOTROUND`、`DWMWA_BORDER_COLOR=NONE`
4. 相机 `clearFlags=SolidColor`，背景 `(0,0,0,0)`；`allowHDR=false`、`allowMSAA=false`
5. 仅 D3D11；`PlayerSettings.useFlipModelSwapchain=false`
6. 不要混用 `WS_EX_LAYERED`/`LWA_ALPHA` 与 DWM alpha

## 已知黑底来源（按优先级）

1. D3D12（swapchain 固定 flip model）→ 强制 D3D11
2. DXGI flip model 未关闭
3. Win11 “窗口化游戏优化”/Auto HDR 升级 swapchain → 对该 exe 关闭
4. HDR / MSAA / 后处理 / 不透明全屏 UI
5. Unity Splash 或其它不透明窗口先绘制
6. 混用 layered / DWM alpha

## DPI

- 不混用 Unity `Screen` 与 Win32 坐标；命中/吸附/跟随全部用 Win32。
- `GetProcessDpiAwareness == 2` 时用物理像素 + DWM extended bounds；
  否则用 `GetWindowRect`，不要混入 DWM bounds（DWM bounds 不做 DPI 虚拟化）。
- 单屏 125/150/200 实测；混合 DPI 双屏阻塞。

## 退出

- `Ctrl+Alt+Q`：`RegisterHotKey` + `WM_HOTKEY`，无焦点可用
- 右键 X 徽标（阶段 2 收尾）：在圆盘 region 内，不改变命中区
- 退出前 `UnregisterHotKey`、恢复 WndProc；`tools/stop-pet.ps1` 兜底

## Unity Personal 限制

- Splash 不能关闭；启动瞬间可能有短暂边框/激活。录屏从 Splash 后开始。
