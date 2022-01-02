param
(
    [Parameter(Mandatory = $true)] [string] $BuildVersion
)

$ErrorActionPreference = 'Stop'

$workingDir = (Resolve-Path -Path "$PSScriptRoot/../tasks").Path
Push-Location -Path $workingDir

$nameTag = "today-in-destiny2-tasks:$BuildVersion"
& docker build --tag $nameTag . --build-arg "BUILD_VERSION=$BuildVersion"

Pop-Location

return $nameTag