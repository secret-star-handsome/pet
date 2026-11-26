param(
    [string]$ProcessName = "xiaosu-pet",
    [string]$OutFile = "E:\xiaosu-pet\Logs\pet-printwindow.png"
)

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DpiAware { [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value); }
'@
[DpiAware]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class PetPrint
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")] public static extern IntPtr GetTopWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int max);
}
'@

$proc = Get-Process -Name $ProcessName -ErrorAction Stop | Select-Object -First 1
[uint32]$targetPid = [uint32]$proc.Id
$hwnd = [IntPtr]::Zero

$h = [PetPrint]::GetTopWindow([IntPtr]::Zero)
while ($h -ne [IntPtr]::Zero) {
    [uint32]$windowPid = 0
    [PetPrint]::GetWindowThreadProcessId($h, [ref]$windowPid) | Out-Null
    if ($windowPid -eq $targetPid -and [PetPrint]::IsWindowVisible($h)) {
        $className = New-Object System.Text.StringBuilder 256
        [PetPrint]::GetClassName($h, $className, 256) | Out-Null
        if ($className.ToString() -eq "UnityWndClass") {
            $hwnd = $h
            break
        }
    }
    $h = [PetPrint]::GetWindow($h, 2)
}

if ($hwnd -eq [IntPtr]::Zero) {
    Write-Host "Pet window not found."
    exit 1
}

$rect = New-Object PetPrint+RECT
[PetPrint]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
Write-Host ("hwnd=0x{0:X} rect=({1},{2}) size={3}x{4}" -f $hwnd.ToInt64(), $rect.Left, $rect.Top, $width, $height)

$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$hdc = $graphics.GetHdc()
$ok = [PetPrint]::PrintWindow($hwnd, $hdc, 2)
$graphics.ReleaseHdc($hdc)
$graphics.Dispose()
$bitmap.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

Write-Host ("PrintWindow={0} saved={1}" -f $ok, $OutFile)
