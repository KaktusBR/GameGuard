#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$pub  = Join-Path $root "publish"

dotnet publish "$root/src/GameGuard.Service" -c Release -r win-x64 --self-contained -o "$pub/service"
dotnet publish "$root/src/GameGuard.Agent"   -c Release -r win-x64 --self-contained -o "$pub/agent"
dotnet publish "$root/src/GameGuard.Admin"   -c Release -r win-x64 --self-contained -o "$pub/admin"

$svcExe = Join-Path $pub "service/GameGuard.Service.exe"
sc.exe create GameGuard binPath= "`"$svcExe`"" start= auto obj= LocalSystem DisplayName= "GameGuard"
# Restart on crash: 1st/2nd/subsequent failures restart after 5s
sc.exe failure GameGuard reset= 0 actions= restart/5000/restart/5000/restart/5000
sc.exe start GameGuard

# Agent at logon for all users (runs in the interactive session)
$agentExe = Join-Path $pub "agent/GameGuard.Agent.exe"
$action  = New-ScheduledTaskAction -Execute $agentExe
$trigger = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal -GroupId "S-1-5-32-545" -RunLevel Limited # BUILTIN\Users
Register-ScheduledTask -TaskName "GameGuardAgent" -Action $action -Trigger $trigger -Principal $principal -Force

Write-Host "Installed. Run the Admin tool to set the parent code:`n  $($pub)\admin\GameGuard.Admin.exe"
