param(
    [string]$ProcessName = "xiaosu-pet",
    [int]$X = 100,
    [int]$Y = 100
)

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

public static class PetMove
{
    [DllImport("user32.dll")] public static extern IntPtr GetTopWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int max);
}
'@

$proc = Get-Process -Name $ProcessName -ErrorAction Stop | Select-Object -First 1
[uint32]$targetPid = [uint32]$proc.Id
$hwnd = [IntPtr]::Zero
$h = [PetMove]::GetTopWindow([IntPtr]::Zero)
while ($h -ne [IntPtr]::Zero) {
    [uint32]$windowPid = 0
    [PetMove]::GetWindowThreadProcessId($h, [ref]$windowPid) | Out-Null
    if ($windowPid -eq $targetPid -and [PetMove]::IsWindowVisible($h)) {
        $className = New-Object System.Text.StringBuilder 256
        [PetMove]::GetClassName($h, $className, 256) | Out-Null
        if ($className.ToString() -eq "UnityWndClass") {
            $hwnd = $h
            break
        }
    }
    $h = [PetMove]::GetWindow($h, 2)
}

if ($hwnd -eq [IntPtr]::Zero) {
    Write-Host "Pet window not found."
    exit 1
}

$flags = 0x0001 -bor 0x0010 -bor 0x0200 # NOSIZE | NOACTIVATE | NOOWNERZORDER
[PetMove]::SetWindowPos($hwnd, [IntPtr](-1), $X, $Y, 0, 0, $flags) | Out-Null
Write-Host ("Moved hwnd=0x{0:X} to {1},{2}" -f $hwnd.ToInt64(), $X, $Y)
