param
(
    [Parameter(Mandatory = $true)] [string] $RepoName,
    [Parameter(Mandatory = $true)] [string] $RepoUri
)

$ErrorActionPreference = 'Stop'

$tag = 'dummy'

$res = & aws ecr list-images --repository-name $RepoName --query "imageIds[?imageTag=='$tag']"
if ($res | ConvertFrom-Json) {
    Write-Host "Dummy image already found. Skipping push."
}
else {
    Write-Host "Dummy image was not found. Pushing to ECR..."
    $repoHost = $RepoUri.Substring(0, $RepoUri.IndexOf('/'))
    $imageUri = "$($RepoUri):$tag"
    & aws ecr get-login-password | docker login --username AWS --password-stdin $repoHost
    & docker pull hello-world:latest
    & docker tag hello-world:latest $imageUri
    & docker push $imageUri
}