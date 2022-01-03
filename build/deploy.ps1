param
(
    [Parameter(Mandatory = $true)] [string] $WebsiteS3Uri,
    [Parameter(Mandatory = $true)] [string] $WebsiteCloudFrontDistributionId,
    [Parameter(Mandatory = $true)] [string] $DataSourceUri,
    [Parameter(Mandatory = $true)] [string] $TasksContainerRepoUri,
    [Parameter(Mandatory = $true)] [string] $DockerImageNameTag,
    [Parameter(Mandatory = $true)] [string] $TestLambdaFunctionArn
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
$repoHost = $TasksContainerRepoUri.Substring(0, $TasksContainerRepoUri.IndexOf('/'))
Write-Host "Pushing tasks container image..."
Write-Host "  Local  : $DockerImageNameTag"
Write-Host "  Remote : $remoteImageNameTag"
& aws ecr get-login-password | docker login --username AWS --password-stdin $repoHost
& docker tag $DockerImageNameTag $remoteImageNameTag
& docker push $remoteImageNameTag

Write-Host "Updating Lambda function to use new container image..."
& aws lambda update-function-code `
    --function-name $TestLambdaFunctionArn `
    --image-uri $remoteImageNameTag