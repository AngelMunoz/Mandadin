Write-Host "Deleting 'dist' Directory";
Remove-Item -Recurse "./dist" -ErrorAction SilentlyContinue;
Write-Host "Building Release";
dotnet publish -c Release -o dist;
if ($LASTEXITCODE -gt 0) {
    Stop-Process -Name "Failed To build" -ErrorAction Stop
}
Write-Host "Publishing";
firebase deploy;