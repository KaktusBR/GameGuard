#Requires -RunAsAdministrator

# Stop and delete the service — tolerate failure when already gone
try { sc.exe stop GameGuard } catch {}
try { sc.exe delete GameGuard } catch {}

# Remove the scheduled task — tolerate failure when already gone
Unregister-ScheduledTask -TaskName "GameGuardAgent" -Confirm:$false -ErrorAction SilentlyContinue

# Strip the GameGuard block from hosts — surface any real write failures
$hosts = "$env:SystemRoot\System32\drivers\etc\hosts"
if (Test-Path $hosts) {
    try {
        $content = Get-Content $hosts -Raw
        $cleaned = [System.Text.RegularExpressions.Regex]::Replace(
            $content, "# BEGIN GameGuard.*?# END GameGuard\r?\n?", "",
            [System.Text.RegularExpressions.RegexOptions]::Singleline)
        Set-Content -Path $hosts -Value $cleaned -Encoding ascii -ErrorAction Stop
    } catch {
        Write-Warning "Failed to clean hosts file: $_"
    }
}

Write-Host "Uninstalled. Config left at C:\ProgramData\GameGuard (delete manually if desired)."
