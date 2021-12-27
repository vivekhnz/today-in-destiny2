/*
  Pass AWS backend configuration via CLI args e.g.

  terraform init `
    -backend-config="bucket=<s3_bucket_name>" `
    -backend-config="region=<aws_region_name>" `
    -backend-config="dynamodb_table=<dynamodb_lock_table_name>"
  
  Ensure the following environment variables are set:
  - TF_VAR_domain_name
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

variable "domain_name" {
  type        = string
  description = "The domain name for the website excluding the 'www.' prefix."
}

locals {
  www_s3_bucket_name = "www.${var.domain_name}"
}

provider "aws" {
  region = "us-east-1"
}

resource "aws_s3_bucket" "www_bucket" {
  bucket = local.www_s3_bucket_name
  policy = templatefile("build/templates/s3-policy.json", {
    bucket = local.www_s3_bucket_name
  })
  website {
    index_document = "index.html"
  }
  tags = {}
}
