Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class Hotkey
{
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

$ctrl = 0x11
$alt = 0x12
$q = 0x51
$down = 0
$up = 2

[Hotkey]::keybd_event($ctrl, 0, $down, [UIntPtr]::Zero)
[Hotkey]::keybd_event($alt, 0, $down, [UIntPtr]::Zero)
[Hotkey]::keybd_event($q, 0, $down, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 80
[Hotkey]::keybd_event($q, 0, $up, [UIntPtr]::Zero)
[Hotkey]::keybd_event($alt, 0, $up, [UIntPtr]::Zero)
[Hotkey]::keybd_event($ctrl, 0, $up, [UIntPtr]::Zero)

Write-Host "Sent Ctrl+Alt+Q"
