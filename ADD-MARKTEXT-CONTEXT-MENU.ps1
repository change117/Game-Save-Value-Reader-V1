# Run this script as Administrator (Right-click PowerShell -> Run as Administrator)
# Adds "Open with MarkText" to the top of the .md right-click context menu

$markTextPath = "C:\Program Files\MarkText\MarkText.exe"

# Verify MarkText exists
if (-not (Test-Path $markTextPath)) {
    Write-Host "ERROR: MarkText not found at $markTextPath" -ForegroundColor Red
    Write-Host "Please update the path at the top of this script." -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "Found MarkText at: $markTextPath" -ForegroundColor Green

# 1. Find what ProgID .md is associated with
$progId = (Get-ItemProperty "HKLM:\SOFTWARE\Classes\.md" -ErrorAction SilentlyContinue).'(default)'
$userProgId = (Get-ItemProperty "HKCU:\SOFTWARE\Classes\.md" -ErrorAction SilentlyContinue).'(default)'
$userChoice = (Get-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.md\UserChoice" -ErrorAction SilentlyContinue).ProgId

Write-Host ""
Write-Host "Detected associations:" -ForegroundColor Cyan
Write-Host "  HKLM ProgID:    $progId"
Write-Host "  HKCU ProgID:    $userProgId"  
Write-Host "  UserChoice:     $userChoice"
Write-Host ""

# Collect all unique ProgIDs to add shell entries to
$progIds = @($progId, $userProgId, $userChoice) | Where-Object { $_ -and $_ -ne '' } | Select-Object -Unique

# 2. Add to SystemFileAssociations (always works)
$sfaPath = "HKLM:\SOFTWARE\Classes\SystemFileAssociations\.md\shell\MarkText"
Write-Host "Adding to SystemFileAssociations..." -ForegroundColor Yellow
New-Item -Path "$sfaPath\command" -Force | Out-Null
Set-ItemProperty -Path $sfaPath -Name "(default)" -Value "Open with MarkText"
Set-ItemProperty -Path $sfaPath -Name "Position" -Value "Top"
Set-ItemProperty -Path $sfaPath -Name "Icon" -Value "`"$markTextPath`""
Set-ItemProperty -Path "$sfaPath\command" -Name "(default)" -Value "`"$markTextPath`" `"%1`""
Write-Host "  Done." -ForegroundColor Green

# 3. Add to .md directly
$mdPath = "HKLM:\SOFTWARE\Classes\.md\shell\MarkText"
Write-Host "Adding to .md shell..." -ForegroundColor Yellow
New-Item -Path "$mdPath\command" -Force | Out-Null
Set-ItemProperty -Path $mdPath -Name "(default)" -Value "Open with MarkText"
Set-ItemProperty -Path $mdPath -Name "Position" -Value "Top"
Set-ItemProperty -Path $mdPath -Name "Icon" -Value "`"$markTextPath`""
Set-ItemProperty -Path "$mdPath\command" -Name "(default)" -Value "`"$markTextPath`" `"%1`""
Write-Host "  Done." -ForegroundColor Green

# 4. Add to each detected ProgID
foreach ($id in $progIds) {
    $progPath = "HKLM:\SOFTWARE\Classes\$id\shell\MarkText"
    Write-Host "Adding to ProgID: $id ..." -ForegroundColor Yellow
    New-Item -Path "$progPath\command" -Force | Out-Null
    Set-ItemProperty -Path $progPath -Name "(default)" -Value "Open with MarkText"
    Set-ItemProperty -Path $progPath -Name "Position" -Value "Top"
    Set-ItemProperty -Path $progPath -Name "Icon" -Value "`"$markTextPath`""
    Set-ItemProperty -Path "$progPath\command" -Name "(default)" -Value "`"$markTextPath`" `"%1`""
    Write-Host "  Done." -ForegroundColor Green
}

# 5. Refresh Explorer shell so changes appear immediately
Write-Host ""
Write-Host "Refreshing Explorer shell..." -ForegroundColor Yellow
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Start-Process explorer

Write-Host ""
Write-Host "=== ALL DONE ===" -ForegroundColor Green
Write-Host "Right-click any .md file - 'Open with MarkText' should now be at the top." -ForegroundColor Green
Write-Host ""
pause
