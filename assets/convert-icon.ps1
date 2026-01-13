# Convert PNG to ICO using .NET
param(
    [string]$InputPath = "log.png",
    [string]$OutputPath = "..\src\nLogMonitor.Desktop\icon.ico"
)

Add-Type -AssemblyName System.Drawing

try {
    # Load the PNG image
    $png = [System.Drawing.Image]::FromFile((Resolve-Path $InputPath).Path)

    # Create an Icon from the bitmap
    $bitmap = New-Object System.Drawing.Bitmap $png

    # Create icon with multiple sizes (256, 128, 64, 48, 32, 16)
    $sizes = @(256, 128, 64, 48, 32, 16)
    $icons = @()

    foreach ($size in $sizes) {
        $resizedBitmap = New-Object System.Drawing.Bitmap $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($resizedBitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($bitmap, 0, 0, $size, $size)
        $graphics.Dispose()

        $icons += $resizedBitmap
    }

    # Save as ICO
    $outputFullPath = Join-Path $PSScriptRoot $OutputPath
    $outputDir = Split-Path $outputFullPath -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    }

    # Use the largest bitmap to create the icon
    $icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())

    # Save the icon
    $fs = New-Object System.IO.FileStream $outputFullPath, ([System.IO.FileMode]::Create)
    $icon.Save($fs)
    $fs.Close()

    # Cleanup
    foreach ($ic in $icons) {
        $ic.Dispose()
    }
    $bitmap.Dispose()
    $png.Dispose()
    $icon.Dispose()

    Write-Host "Icon created successfully: $outputFullPath" -ForegroundColor Green

} catch {
    Write-Host "Error creating icon: $_" -ForegroundColor Red
    exit 1
}
