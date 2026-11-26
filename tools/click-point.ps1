param(
    [Parameter(Mandatory = $true)][int]$X,
    [Parameter(Mandatory = $true)][int]$Y,
    [int]$HoldMilliseconds = 60
)

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DpiAware { [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value); }
'@
[DpiAware]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class PetClick
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
}
'@

$before = [PetClick]::GetForegroundWindow()
[PetClick]::SetCursorPos($X, $Y) | Out-Null
Start-Sleep -Milliseconds 500
[PetClick]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds $HoldMilliseconds
[PetClick]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 250
$after = [PetClick]::GetForegroundWindow()

Write-Host ("click=({0},{1}) foreground before=0x{2:X} after=0x{3:X} changed={4}" -f $X, $Y, $before.ToInt64(), $after.ToInt64(), ($before -ne $after))
