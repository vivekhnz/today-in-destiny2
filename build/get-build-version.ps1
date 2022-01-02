$ErrorActionPreference = 'Stop'

$patchVerStr = & git rev-list HEAD --count

[int]$parsed = 0
if (![int]::TryParse($patchVerStr, [ref]$parsed)) {
    throw "Unable to parse patch number: $patchVerStr"
}

# todo: base major and minor components off of the last tagged commit
$major = '0'
$minor = '1'

# --count starts at 1 but we want our patch numbers to start at 0
$patch = $parsed - 1

return "$major.$minor.$patch"