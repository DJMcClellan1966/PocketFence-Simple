# PowerShell script to replace FontWeight with FontAttributes in XAML files
$files = Get-ChildItem -Path "*.xaml"
foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName
    $content = $content -replace 'FontWeight="Bold"', 'FontAttributes="Bold"'
    Set-Content -Path $file.FullName -Value $content
}
Write-Host "FontWeight replaced with FontAttributes in all XAML files"