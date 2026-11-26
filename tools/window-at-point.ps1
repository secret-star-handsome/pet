param(
    [Parameter(Mandatory = $true)][int]$X,
    [Parameter(Mandatory = $true)][int]$Y
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

public static class PointProbe
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
}
'@

$point = New-Object PointProbe+POINT
$point.X = $X
$point.Y = $Y
$hwnd = [PointProbe]::WindowFromPoint($point)
$root = [PointProbe]::GetAncestor($hwnd, 2)

[uint32]$procId = 0
[PointProbe]::GetWindowThreadProcessId($root, [ref]$procId) | Out-Null

$className = New-Object System.Text.StringBuilder 256
[PointProbe]::GetClassName($root, $className, 256) | Out-Null
$title = New-Object System.Text.StringBuilder 256
[PointProbe]::GetWindowText($root, $title, 256) | Out-Null

Write-Host ("point=({0},{1}) hwnd=0x{2:X} root=0x{3:X} pid={4} class={5} title='{6}'" -f $X, $Y, $hwnd.ToInt64(), $root.ToInt64(), $procId, $className.ToString(), $title.ToString())
