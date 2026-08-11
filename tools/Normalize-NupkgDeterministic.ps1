# Normalize-NupkgDeterministic.ps1
#
# 使 dotnet pack 产出的 .nupkg 字节可复现。
#
# 背景：NuGet pack 会在 core-properties 下生成一个文件名随机的 *.psmdcp，
# 且直接以文件系统时间戳/枚举顺序写 zip，导致同一源码两次 pack 的 SHA-256 不同。
# 本脚本对每个 nupkg 做确定性归一化：
#   1) 把 core-properties/*.psmdcp 重命名为固定名（默认 'package.psmdcp'）；
#   2) 重写 _rels/.rels 中指向该文件的 Target；
#      （[Content_Types].xml 用 Default Extension="psmdcp"，无需改）
#   3) 按名称排序、固定时间戳、固定压缩级别重新写 zip。
#
# 用法：
#   pwsh tools/Normalize-NupkgDeterministic.ps1 -PackagesDir artifacts/packages
#   pwsh tools/Normalize-NupkgDeterministic.ps1 -PackagesDir artifacts/packages -Verbose
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagesDir,

    # 固定 core-properties 文件名（不含扩展名），默认 'package'
    [string]$FixedPsmdcpName = 'package',

    # zip 条目固定时间戳（UTC），默认 2026-01-01T00:00:00Z
    [DateTime]$FixedTimestampUtc = (Get-Date '2026-01-01T00:00:00Z'),

    # core-properties 关系固定 Id（NuGet 每次生成随机 16 位 Id = "R" + 15 位十六进制）
    [string]$FixedCorePropsRelationshipId = '0000000000000002'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Nupkg {
    param([string]$NupkgPath)

    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ('nupkg-norm-' + [guid]::NewGuid().ToString('N'))
    New-Item -Path $temp -ItemType Directory -Force | Out-Null
    try {
        # 1) 解包
        $zip = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
        try {
            foreach ($entry in $zip.Entries) {
                $dest = Join-Path $temp ($entry.FullName -replace '/', [System.IO.Path]::DirectorySeparatorChar)
                $dir = [System.IO.Path]::GetDirectoryName($dest)
                if ($dir -and -not (Test-Path $dir)) { New-Item -Path $dir -ItemType Directory -Force | Out-Null }
                [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $dest, $true)
            }
        } finally {
            $zip.Dispose()
        }

        # 2) 归一化 psmdcp 文件名 + 修正 .rels
        $corePropsDir = Join-Path $temp 'package/services/metadata/core-properties'
        if (Test-Path $corePropsDir) {
            $psmdcpFiles = @(Get-ChildItem -Path $corePropsDir -Filter '*.psmdcp' -ErrorAction SilentlyContinue)
            $fixedBase = $FixedPsmdcpName + '.psmdcp'
            foreach ($f in $psmdcpFiles) {
                if ($f.Name -ne $fixedBase) {
                    Move-Item -Path $f.FullName -Destination (Join-Path $corePropsDir $fixedBase) -Force
                }
            }

            $relsPath = Join-Path $temp '_rels/.rels'
            if (Test-Path $relsPath) {
                $rels = [System.IO.File]::ReadAllText($relsPath)
                $rels = [regex]::Replace(
                    $rels,
                    'core-properties/[0-9a-fA-F]+\.psmdcp',
                    ('core-properties/' + $fixedBase))
                # NuGet 为 core-properties 关系生成随机 16 位 Id（"R" + 15 位十六进制），同样归一化为固定值
                $rels = [regex]::Replace(
                    $rels,
                    ('(Target="/package/services/metadata/core-properties/' +
                        [regex]::Escape($fixedBase) + '" Id="R)[0-9A-F]+"'),
                    ('${1}' + $FixedCorePropsRelationshipId + '"'))
                [System.IO.File]::WriteAllText($relsPath, $rels, $Utf8NoBom)
            }
        }

        # 3) 确定性重打包：按名称排序、固定时间戳、固定压缩级别
        $files = @(Get-ChildItem -Path $temp -Recurse -File | Sort-Object FullName)
        $tmpOut = $NupkgPath + '.normalized.tmp'
        if (Test-Path $tmpOut) { Remove-Item $tmpOut -Force }

        $stream = [System.IO.File]::Open($tmpOut, [System.IO.FileMode]::CreateNew)
        $outZip = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            foreach ($f in $files) {
                $relPath = $f.FullName.Substring($temp.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
                $relPath = $relPath -replace '\\', '/'
                $entry = $outZip.CreateEntry($relPath, [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $FixedTimestampUtc
                $inStream = [System.IO.File]::OpenRead($f.FullName)
                try {
                    $outStream = $entry.Open()
                    try {
                        $inStream.CopyTo($outStream)
                    } finally {
                        $outStream.Dispose()
                    }
                } finally {
                    $inStream.Dispose()
                }
            }
        } finally {
            $outZip.Dispose()
            $stream.Dispose()
        }

        Move-Item -Path $tmpOut -Destination $NupkgPath -Force
        Write-Verbose "normalized: $NupkgPath"
    } finally {
        if (Test-Path $temp) { Remove-Item -Path $temp -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

if (-not (Test-Path $PackagesDir)) {
    throw "Packages directory not found: $PackagesDir"
}

$nupkgs = @(Get-ChildItem -Path $PackagesDir -Filter '*.nupkg' | Sort-Object Name)
if ($nupkgs.Count -eq 0) {
    throw "No .nupkg files found in $PackagesDir"
}

foreach ($n in $nupkgs) {
    Normalize-Nupkg -NupkgPath $n.FullName
}

Write-Output "Normalized $($nupkgs.Count) package(s) under $PackagesDir"