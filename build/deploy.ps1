param
(
    [Parameter(Mandatory = $true)] [string] $WebsiteS3Uri,
    [Parameter(Mandatory = $true)] [string] $WebsiteCloudFrontDistributionId
)

$ErrorActionPreference = 'Stop'

$srcDir = (Resolve-Path -Path "$PSScriptRoot/../app/dist").Path
Write-Host "Syncing static website content to S3 bucket..."
Write-Host "  Source      : $srcDir"
Write-Host "  Destination : $WebsiteS3Uri"
& aws s3 sync "$srcDir" $WebsiteS3Uri

Write-Host "Invalidating CloudFront distribution..."
& aws cloudfront create-invalidation `
    --distribution-id $WebsiteCloudFrontDistributionId `
    --paths "/*"