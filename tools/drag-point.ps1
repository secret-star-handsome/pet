param(
    [Parameter(Mandatory = $true)][int]$StartX,
    [Parameter(Mandatory = $true)][int]$StartY,
    [Parameter(Mandatory = $true)][int]$EndX,
    [Parameter(Mandatory = $true)][int]$EndY,
    [int]$Steps = 12,
    [int]$StepDelayMs = 20,
    [string]$ProcessName = "xiaosu-pet"
)

Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class PetDrag
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern IntPtr GetTopWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int max);
}
'@

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DpiAware { [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value); }
'@
[DpiAware]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

function Find-PetWindow([uint32]$targetPid) {
    $h = [PetDrag]::GetTopWindow([IntPtr]::Zero)
    while ($h -ne [IntPtr]::Zero) {
        [uint32]$windowPid = 0
        [PetDrag]::GetWindowThreadProcessId($h, [ref]$windowPid) | Out-Null
        if ($windowPid -eq $targetPid -and [PetDrag]::IsWindowVisible($h)) {
            $className = New-Object System.Text.StringBuilder 256
            [PetDrag]::GetClassName($h, $className, 256) | Out-Null
            if ($className.ToString() -eq "UnityWndClass") { return $h }
        }
        $h = [PetDrag]::GetWindow($h, 2)
    }
    return [IntPtr]::Zero
}

$proc = Get-Process -Name $ProcessName -ErrorAction Stop | Select-Object -First 1
$hwnd = Find-PetWindow ([uint32]$proc.Id)
if ($hwnd -eq [IntPtr]::Zero) {
    Write-Host "Pet window not found."
    exit 1
}

$beforeRect = New-Object PetDrag+RECT
[PetDrag]::GetWindowRect($hwnd, [ref]$beforeRect) | Out-Null
$beforeForeground = [PetDrag]::GetForegroundWindow()

[PetDrag]::SetCursorPos($StartX, $StartY) | Out-Null
Start-Sleep -Milliseconds 250
[PetDrag]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)

for ($i = 1; $i -le $Steps; $i++) {
    $t = $i / [double]$Steps
    $x = [int][Math]::Round($StartX + ($EndX - $StartX) * $t)
    $y = [int][Math]::Round($StartY + ($EndY - $StartY) * $t)
    [PetDrag]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds $StepDelayMs
}

[PetDrag]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 350

$afterRect = New-Object PetDrag+RECT
[PetDrag]::GetWindowRect($hwnd, [ref]$afterRect) | Out-Null
$afterForeground = [PetDrag]::GetForegroundWindow()

Write-Host ("drag=({0},{1})->({2},{3})" -f $StartX, $StartY, $EndX, $EndY)
Write-Host ("beforeRect=({0},{1})-({2},{3}) afterRect=({4},{5})-({6},{7})" -f $beforeRect.Left, $beforeRect.Top, $beforeRect.Right, $beforeRect.Bottom, $afterRect.Left, $afterRect.Top, $afterRect.Right, $afterRect.Bottom)
Write-Host ("foreground before=0x{0:X} after=0x{1:X} changed={2}" -f $beforeForeground.ToInt64(), $afterForeground.ToInt64(), ($beforeForeground -ne $afterForeground))
