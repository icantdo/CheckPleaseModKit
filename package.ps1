<#
  Yayin paketlerini uretir (dist/ klasorune):
    CheckPlease.ModKit-vX.Y.Z.zip          -> oyun klasorune cikarilir (BepInEx/plugins/CheckPlease.ModKit/...)
    CheckPlease.ModKit-Samples-vX.Y.Z.zip  -> ornek modlar (istege bagli)

  Kullanim:  ./package.ps1            (oyun yolu farkliysa: ./package.ps1 -GameDir "D:\Oyunlar\Check Please! Demo")
  Oyunun kendi DLL'leri pakete GIRMEZ.
#>
param([string]$GameDir = "")

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$extra = @()
if ($GameDir) { $extra += "-p:GameDir=$GameDir" }

# Surum numarasini kaynaktan oku (tek dogru kaynak: PluginInfo.Version)
$src = Get-Content "$root\CheckPlease.ModKit\Core\ModKitPlugin.cs" -Raw
if ($src -notmatch 'Version\s*=\s*"([^"]+)"') { throw "PluginInfo.Version bulunamadi" }
$ver = $Matches[1]
Write-Host "Surum: $ver" -ForegroundColor Cyan

function Build($proj) {
    Write-Host "Derleniyor: $proj"
    & dotnet build "$root\$proj" -c Release @extra --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Derleme basarisiz: $proj" }
}

# Zip'i capraz platform uyumlu (ileri egik cizgili) girdilerle yazar
function New-Zip($zipPath, $stageDir) {
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
    try {
        $base = (Resolve-Path $stageDir).Path.TrimEnd('\')
        foreach ($f in Get-ChildItem $stageDir -Recurse -File) {
            $rel = $f.FullName.Substring($base.Length + 1).Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $rel, 'Optimal')
        }
    } finally { $zip.Dispose() }
}

$dist = "$root\dist"
$stage = "$dist\_stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $dist | Out-Null

# ---- ModKit paketi ----
Build "CheckPlease.ModKit"
$out = "$root\CheckPlease.ModKit\bin\Release\net472"
$plug = "$stage\core\BepInEx\plugins\CheckPlease.ModKit"
New-Item -ItemType Directory -Force $plug | Out-Null
Copy-Item "$out\CheckPlease.ModKit.dll" $plug
if (Test-Path "$out\CheckPlease.ModKit.xml") { Copy-Item "$out\CheckPlease.ModKit.xml" $plug }
Copy-Item "$root\README.md", "$root\LICENSE", "$root\CHANGELOG.md" $plug
Copy-Item "$root\docs\MODDING_JSON.md", "$root\docs\MODDING_CSHARP.md" $plug
New-Zip "$dist\CheckPlease.ModKit-v$ver.zip" "$stage\core"

# ---- Ornek modlar ----
$samples = @("TipBoost", "ModBurger", "Behavior")
foreach ($s in $samples) {
    Build "Samples\$s"
    $d = "$stage\samples\BepInEx\plugins\$s"
    New-Item -ItemType Directory -Force $d | Out-Null
    Copy-Item "$root\Samples\$s\bin\Release\net472\$s.dll" $d
}
$jd = "$stage\samples\BepInEx\plugins\JsonSample"
New-Item -ItemType Directory -Force $jd | Out-Null
Copy-Item "$root\Samples\JsonMod\*" $jd -Include "mod.json", "*.png"
New-Zip "$dist\CheckPlease.ModKit-Samples-v$ver.zip" "$stage\samples"

Remove-Item $stage -Recurse -Force
Write-Host "`nHazir:" -ForegroundColor Green
Get-ChildItem $dist -Filter *.zip | ForEach-Object { "{0,-50} {1,8:N0} KB" -f $_.Name, ($_.Length / 1KB) }
