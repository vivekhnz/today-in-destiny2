/*
  Pass AWS backend configuration via CLI args e.g.

  terraform init `
    -backend-config="bucket=<s3_bucket_name>" `
    -backend-config="region=<aws_region_name>" `
    -backend-config="dynamodb_table=<dynamodb_lock_table_name>"
*/

terraform {
  required_version = "~> 1.1.2"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 3.0"
    }
  }

  backend "s3" {
    key     = "terraform.tfstate"
    encrypt = true
  }
}

provider "aws" {
  region = "us-east-1"
}
