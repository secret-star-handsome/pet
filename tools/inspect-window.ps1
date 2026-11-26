param([string]$ProcessName = "xiaosu-pet")

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DpiAware { [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value); }
'@
[DpiAware]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class WinProbe
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern IntPtr GetTopWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")] public static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateRectRgn(int l, int t, int r, int b);
    [DllImport("gdi32.dll")] public static extern int GetRgnBox(IntPtr hRgn, out RECT rect);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("shcore.dll")] public static extern int GetProcessDpiAwareness(IntPtr p, out int value);
}
'@

$procs = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
if (-not $procs) {
    Write-Host ("No process named '{0}'." -f $ProcessName)
    exit 1
}

$targetPids = @($procs | ForEach-Object { [uint32]$_.Id })
$foreground = [WinProbe]::GetForegroundWindow()
Write-Host ("Foreground hwnd=0x{0:X}" -f $foreground.ToInt64())

$h = [WinProbe]::GetTopWindow([IntPtr]::Zero)
while ($h -ne [IntPtr]::Zero) {
    [uint32]$procId = 0
    [WinProbe]::GetWindowThreadProcessId($h, [ref]$procId) | Out-Null

    if ($targetPids -contains $procId) {
        $rect = New-Object WinProbe+RECT
        [WinProbe]::GetWindowRect($h, [ref]$rect) | Out-Null
        $style = [WinProbe]::GetWindowLongPtr($h, -16).ToInt64()
        $exStyle = [WinProbe]::GetWindowLongPtr($h, -20).ToInt64()
        $className = New-Object System.Text.StringBuilder 256
        [WinProbe]::GetClassName($h, $className, 256) | Out-Null
        $title = New-Object System.Text.StringBuilder 256
        [WinProbe]::GetWindowText($h, $title, 256) | Out-Null
        $dpi = [WinProbe]::GetDpiForWindow($h)

        $rgn = [WinProbe]::CreateRectRgn(0, 0, 0, 0)
        $rgnType = [WinProbe]::GetWindowRgn($h, $rgn)
        $rgnBox = New-Object WinProbe+RECT
        $rgnBoxResult = [WinProbe]::GetRgnBox($rgn, [ref]$rgnBox)
        [WinProbe]::DeleteObject($rgn) | Out-Null

        Write-Host ""
        Write-Host ("pid={0} hwnd=0x{1:X} class={2} title='{3}'" -f $procId, $h.ToInt64(), $className.ToString(), $title.ToString())
        Write-Host ("  rect=({0},{1})-({2},{3}) size={4}x{5} dpi={6}" -f $rect.Left, $rect.Top, $rect.Right, $rect.Bottom, ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top), $dpi)
        Write-Host ("  style=0x{0:X} exStyle=0x{1:X} topmost={2} noactivate={3} toolwindow={4}" -f $style, $exStyle, (($exStyle -band 0x8) -ne 0), (($exStyle -band 0x08000000) -ne 0), (($exStyle -band 0x80) -ne 0))
        Write-Host ("  windowRegionType={0} regionBoxResult={1} regionBox=({2},{3})-({4},{5})" -f $rgnType, $rgnBoxResult, $rgnBox.Left, $rgnBox.Top, $rgnBox.Right, $rgnBox.Bottom)
        Write-Host ("  isForeground={0}" -f ($h -eq $foreground))
    }

    $h = [WinProbe]::GetWindow($h, 2) # GW_HWNDNEXT
}
