# Builds Partnerly Agent (the program users install on their PC) as ONE self-contained file,
# so nothing else has to be installed on Windows, and puts it where the site offers it for download.
#
#   powershell -File tools/publish-agent.ps1
#   powershell -File tools/publish-agent.ps1 -Output "D:\agent"     # other folder (set Agent:DownloadFolder to it)
param(
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $root "Partnerly/App_Data/agent"
}

dotnet publish (Join-Path $root "Partnerly.Agent/Partnerly.Agent.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $Output --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Only the exe is offered for download.
Get-ChildItem $Output -File | Where-Object { $_.Name -ne "Partnerly.Agent.exe" } | Remove-Item -Force
$size = [math]::Round((Get-Item (Join-Path $Output "Partnerly.Agent.exe")).Length / 1MB, 1)
Write-Host "Done: $Output/Partnerly.Agent.exe ($size MB)"
