$ErrorActionPreference = "Stop"

$output = Join-Path $PSScriptRoot "results"

if (Test-Path $output) {
    Remove-Item -Recurse -Force $output
}

dotnet run -- --output=$output
