param([string]$ProcessName = "xiaosu-pet")

Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host ("Stopping {0} (pid {1})" -f $_.ProcessName, $_.Id)
    Stop-Process -Id $_.Id -Force
}

Get-Process -Name "UnityCrashHandler64" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Path -like "*xiaosu*") {
        Write-Host ("Stopping crash handler (pid {0})" -f $_.Id)
        Stop-Process -Id $_.Id -Force
    }
}
