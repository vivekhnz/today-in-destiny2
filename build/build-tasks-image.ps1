param
(
    [Parameter(Mandatory = $true)] [string] $BuildVersion
)

$ErrorActionPreference = 'Stop'

$workingDir = (Resolve-Path -Path "$PSScriptRoot/../tasks").Path
Push-Location -Path $workingDir

$nameTag = $null
try {
    $nameTag = "today-in-destiny2-tasks:$BuildVersion"
    & docker build --tag $nameTag . --build-arg "BUILD_VERSION=$BuildVersion" | Out-Host

    $res = & docker image inspect $nameTag
    if (!($res | ConvertFrom-Json)) {
        throw "Docker image build failed"
    }
}
finally {
    Pop-Location
}

return $nameTag