# Run this script as Administrator to undo MarkText context menu changes
# Removes all "Open with MarkText" entries added by ADD-MARKTEXT-CONTEXT-MENU.ps1

Write-Host "Removing MarkText context menu entries..." -ForegroundColor Yellow

# Remove from SystemFileAssociations
Remove-Item -Path "HKLM:\SOFTWARE\Classes\SystemFileAssociations\.md\shell\MarkText" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  Removed from SystemFileAssociations" -ForegroundColor Green

# Remove from .md directly
Remove-Item -Path "HKLM:\SOFTWARE\Classes\.md\shell\MarkText" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  Removed from .md shell" -ForegroundColor Green

# Remove from common Visual Studio ProgIDs
$progId = (Get-ItemProperty "HKLM:\SOFTWARE\Classes\.md" -ErrorAction SilentlyContinue).'(default)'
$userProgId = (Get-ItemProperty "HKCU:\SOFTWARE\Classes\.md" -ErrorAction SilentlyContinue).'(default)'
$userChoice = (Get-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.md\UserChoice" -ErrorAction SilentlyContinue).ProgId

$progIds = @($progId, $userProgId, $userChoice) | Where-Object { $_ -and $_ -ne '' } | Select-Object -Unique

foreach ($id in $progIds) {
    Remove-Item -Path "HKLM:\SOFTWARE\Classes\$id\shell\MarkText" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Removed from ProgID: $id" -ForegroundColor Green
}

# Refresh Explorer
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Start-Process explorer

Write-Host ""
Write-Host "=== ALL REMOVED ===" -ForegroundColor Green
Write-Host ""
pause
