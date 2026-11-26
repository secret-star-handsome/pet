param(
    [ValidateSet("launch", "rect", "move", "raise", "activate", "minimize", "restore", "close")]
    [string]$Action = "rect",
    [int]$X = 500,
    [int]$Y = 300,
    [int]$Width = 800,
    [int]$Height = 500
)

Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class NotepadCtl
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")] public static extern IntPtr GetTopWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int max);
}
'@

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DpiAware { [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value); }
'@
[DpiAware]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

function Find-NotepadWindow([uint32]$targetPid) {
    $best = [IntPtr]::Zero
    $bestArea = 0
    $h = [NotepadCtl]::GetTopWindow([IntPtr]::Zero)
    while ($h -ne [IntPtr]::Zero) {
        [uint32]$windowPid = 0
        [NotepadCtl]::GetWindowThreadProcessId($h, [ref]$windowPid) | Out-Null
        if ($windowPid -eq $targetPid -and [NotepadCtl]::IsWindowVisible($h)) {
            $rect = New-Object NotepadCtl+RECT
            [NotepadCtl]::GetWindowRect($h, [ref]$rect) | Out-Null
            $area = ($rect.Right - $rect.Left) * ($rect.Bottom - $rect.Top)
            if ($area -gt $bestArea) {
                $bestArea = $area
                $best = $h
            }
        }
        $h = [NotepadCtl]::GetWindow($h, 2)
    }
    return $best
}

if ($Action -eq "launch") {
    $existing = Get-Process -Name notepad -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $existing) {
        Start-Process notepad.exe | Out-Null
        Start-Sleep -Seconds 2
    }
}

$proc = Get-Process -Name notepad -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) {
    Write-Host "Notepad not running."
    exit 1
}

$hwnd = Find-NotepadWindow ([uint32]$proc.Id)
if ($hwnd -eq [IntPtr]::Zero) {
    Write-Host "Notepad window not found."
    exit 1
}

switch ($Action) {
    "launch" { }
    "rect" { }
    "move" {
        [NotepadCtl]::ShowWindow($hwnd, 9) | Out-Null
        $flags = 0x0004 -bor 0x0010 # NOZORDER | NOACTIVATE
        [NotepadCtl]::SetWindowPos($hwnd, [IntPtr]::Zero, $X, $Y, $Width, $Height, $flags) | Out-Null
    }
    "raise" {
        $flags = 0x0001 -bor 0x0002 -bor 0x0010 # NOMOVE | NOSIZE | NOACTIVATE (no NOZORDER)
        [NotepadCtl]::SetWindowPos($hwnd, [IntPtr]::Zero, 0, 0, 0, 0, $flags) | Out-Null
    }
    "activate" {
        [NotepadCtl]::ShowWindow($hwnd, 9) | Out-Null
        try {
            $shell = New-Object -ComObject WScript.Shell
            $shell.AppActivate($proc.Id) | Out-Null
        } catch {
            Write-Host ("AppActivate failed: {0}" -f $_.Exception.Message)
        }
    }
    "minimize" { [NotepadCtl]::ShowWindow($hwnd, 6) | Out-Null }
    "restore" { [NotepadCtl]::ShowWindow($hwnd, 9) | Out-Null }
    "close" { [NotepadCtl]::PostMessage($hwnd, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }
}

Start-Sleep -Milliseconds 300
$rect = New-Object NotepadCtl+RECT
[NotepadCtl]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
Write-Host ("notepad action={0} hwnd=0x{1:X} rect=({2},{3})-({4},{5}) size={6}x{7}" -f $Action, $hwnd.ToInt64(), $rect.Left, $rect.Top, $rect.Right, $rect.Bottom, ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top))
