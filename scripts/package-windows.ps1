param(
    [Parameter(Mandatory = $true)]
    [string]$Runtime,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())

try {
    dotnet publish (Join-Path $projectRoot 'src/SumScription.App/SumScription.App.csproj') `
        --configuration Release `
        --runtime $Runtime `
        --self-contained true `
        --output $publishDirectory `
        -p:Version=$Version `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $architecture = $Runtime -replace '^win-', ''
    $archive = Join-Path $OutputDirectory "SumScription-Windows-$architecture-v$Version.zip"
    Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $archive -Force
    Write-Output $archive
}
finally {
    if (Test-Path $publishDirectory) {
        Remove-Item -Recurse -Force $publishDirectory
    }
}
