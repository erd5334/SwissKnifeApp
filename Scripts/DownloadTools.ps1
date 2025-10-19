# Advanced Installer Custom Action Script
# Bu script kurulum sırasında çalışarak gerekli araçları indirir

param(
    [string]$TargetDir = "$env:APPDATA\SwissKnifeApp\Tools"
)

$ErrorActionPreference = "Stop"

# Hedef klasörü oluştur
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

Write-Host "Gerekli araçlar indiriliyor: $TargetDir"

# yt-dlp indirme
$ytdlpPath = Join-Path $TargetDir "yt-dlp.exe"
if (-Not (Test-Path $ytdlpPath)) {
    Write-Host "yt-dlp indiriliyor..."
    try {
        $ytdlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
        Invoke-WebRequest -Uri $ytdlpUrl -OutFile $ytdlpPath -UseBasicParsing
        Write-Host "✓ yt-dlp indirildi: $ytdlpPath"
    }
    catch {
        Write-Warning "yt-dlp indirilemedi: $_"
    }
}
else {
    Write-Host "✓ yt-dlp zaten mevcut"
}

# ffmpeg indirme (essentials paketi)
$ffmpegPath = Join-Path $TargetDir "ffmpeg.exe"
if (-Not (Test-Path $ffmpegPath)) {
    Write-Host "ffmpeg indiriliyor..."
    try {
        # Geçici zip indirme
        $ffmpegZipUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
        $tempZip = Join-Path $env:TEMP "ffmpeg.zip"
        $tempExtract = Join-Path $env:TEMP "ffmpeg_extract"
        
        Write-Host "  İndiriliyor... (Bu biraz sürebilir)"
        Invoke-WebRequest -Uri $ffmpegZipUrl -OutFile $tempZip -UseBasicParsing
        
        Write-Host "  Çıkarılıyor..."
        Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
        
        # ffmpeg.exe'yi bul ve kopyala
        $ffmpegExe = Get-ChildItem -Path $tempExtract -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1
        if ($ffmpegExe) {
            Copy-Item $ffmpegExe.FullName -Destination $ffmpegPath
            Write-Host "✓ ffmpeg indirildi: $ffmpegPath"
        }
        
        # Temizlik
        Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
        Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
    }
    catch {
        Write-Warning "ffmpeg indirilemedi: $_"
    }
}
else {
    Write-Host "✓ ffmpeg zaten mevcut"
}

Write-Host ""
Write-Host "Kurulum tamamlandı!"
Write-Host "Araçlar konumu: $TargetDir"

# Başarı kontrolü
$success = (Test-Path $ytdlpPath) -and (Test-Path $ffmpegPath)
if ($success) {
    exit 0
}
else {
    Write-Warning "Bazı araçlar indirilemedi. Program yine de çalışacak ama YouTube modülü için araçları manuel kurmanız gerekebilir."
    exit 0  # Kurulumu durdurmamak için yine de 0 dön
}
