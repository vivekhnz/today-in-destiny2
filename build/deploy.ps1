param
(
    [Parameter(Mandatory = $true)] [string] $WebsiteS3Uri,
    [Parameter(Mandatory = $true)] [string] $WebsiteCloudFrontDistributionId,
    [Parameter(Mandatory = $true)] [string] $DataSourceUri,
    [Parameter(Mandatory = $true)] [string] $TasksContainerRepoUri,
    [Parameter(Mandatory = $true)] [string] $DockerImageNameTag
)

$ErrorActionPreference = 'Stop'

$srcDir = (Resolve-Path -Path "$PSScriptRoot/../app/dist").Path

Write-Host "Updating data source URI in index file..."
$indexHtmlPath = Join-Path $srcDir 'index.html'
$origHtml = Get-Content -Path $indexHtmlPath -Raw
$modifiedHtml = $origHtml -replace 'data-source="/__data"', "data-source=`"$DataSourceUri`""
$modifiedHtml | Out-File -Path $indexHtmlPath -Encoding utf8

Write-Host "Syncing static website content to S3 bucket..."
Write-Host "  Source      : $srcDir"
Write-Host "  Destination : $WebsiteS3Uri"
& aws s3 sync "$srcDir" $WebsiteS3Uri

Write-Host "Invalidating CloudFront distribution..."
& aws cloudfront create-invalidation `
    --distribution-id $WebsiteCloudFrontDistributionId `
    --paths "/*"

$tag = $DockerImageNameTag.Substring($DockerImageNameTag.LastIndexOf(':') + 1)
$remoteImageNameTag = "$($TasksContainerRepoUri):$tag"
Write-Host "Pushing tasks container image..."
Write-Host "  Local  : $DockerImageNameTag"
Write-Host "  Remote : $remoteImageNameTag"

& aws ecr-public get-login-password --region us-east-1 | docker login --username AWS --password-stdin public.ecr.aws
& docker tag $DockerImageNameTag $remoteImageNameTag
& docker push $remoteImageNameTag