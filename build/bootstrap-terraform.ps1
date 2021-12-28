param
(
    [Parameter(Mandatory=$true)] [string] $StateBucketName,
    [Parameter(Mandatory=$true)] [string] $LockStateTableName,
    [Parameter(Mandatory=$true)] [string] $AwsRegion
)

$ErrorActionPreference = 'Stop'

function ConvertTo-AwsJson {
    param
    (
        [Parameter(ValueFromPipeline = $true)]
        $InputObject
    )

    $json = $InputObject | ConvertTo-Json -Depth 100 -Compress

    # replace \ with \\
    $escaped = $json -replace '\\', '\\'
    # replace " with \"
    $escaped = $escaped -replace '"', '\"'

    return $escaped
}

# create S3 bucket to hold Terraform state
Write-Host "Retrieving AWS S3 buckets..."
$bucketsJson = & aws s3api list-buckets --output json
if (!$bucketsJson) {
    throw "Unable to retrieve S3 buckets from AWS."
}
if ($bucketsJson) {
    $res = $bucketsJson | ConvertFrom-Json
    $tfStateBucket = $res.Buckets | Where-Object { $_.Name -eq $StateBucketName }
    if ($tfStateBucket) {
        Write-Host "Terraform state bucket already exists."
    }
    else {
        Write-Host "Creating Terraform state bucket..."
        & aws s3api create-bucket `
            --bucket $StateBucketName `
            --region $AwsRegion
        Write-Host "Enabling versioning on state bucket..."
        & aws s3api put-bucket-versioning `
            --bucket $StateBucketName `
            --versioning-configuration 'Status=Enabled'
        Write-Host "Enabling encryption on state bucket..."
        $encryptionConfig = @{
            Rules = @(
                @{
                    ApplyServerSideEncryptionByDefault = @{
                        SSEAlgorithm = 'AES256'
                    }
                }
            )
        }
        & aws s3api put-bucket-encryption `
            --bucket $StateBucketName `
            --server-side-encryption-configuration ($encryptionConfig | ConvertTo-AwsJson)
    }
}

# create DynamoDB table for Terraform lock state
Write-Host "Retrieving AWS DynamoDB tables..."
$tablesJson = & aws dynamodb list-tables --output json
if (!$tablesJson) {
    throw "Unable to retrieve DynamoDB tables from AWS."
}
if ($tablesJson) {
    $res = $tablesJson | ConvertFrom-Json
    $tfLockStateTable = $res.TableNames | Where-Object { $_ -eq $LockStateTableName }
    if ($tfLockStateTable) {
        Write-Host "Terraform lock state table already exists."
    }
    else {
        Write-Host "Creating Terraform lock state table..."
        & aws dynamodb create-table `
            --table-name $LockStateTableName `
            --attribute-definitions 'AttributeName=LockID, AttributeType=S' `
            --key-schema 'AttributeName=LockID, KeyType=HASH' `
            --provisioned-throughput 'ReadCapacityUnits=1, WriteCapacityUnits=1'
    }
}