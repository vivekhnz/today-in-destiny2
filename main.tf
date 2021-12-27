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
  www_domain_name     = "www.${var.domain_name}"
  www_s3_bucket_name  = local.www_domain_name
  root_s3_bucket_name = var.domain_name
  common_tags = {
    Project = var.domain_name
  }
}

provider "aws" {
  region = "us-east-1"
}

// S3 buckets
resource "aws_s3_bucket" "www_bucket" {
  bucket = local.www_s3_bucket_name
  policy = templatefile("build/templates/s3-policy.json", { bucket = local.www_s3_bucket_name })
  website {
    index_document = "index.html"
  }
  tags = local.common_tags
}
resource "aws_s3_bucket" "root_bucket" {
  bucket = local.root_s3_bucket_name
  policy = templatefile("build/templates/s3-policy.json", { bucket = local.root_s3_bucket_name })
  website {
    redirect_all_requests_to = "http://${local.www_domain_name}"
  }
  tags = local.common_tags
}

// Route 53 configuration
resource "aws_route53_zone" "site_zone" {
  name = var.domain_name
  // don't destroy the hosted zone as its nameservers are referenced by the domain registrar
  lifecycle {
    prevent_destroy = true
  }
  tags = local.common_tags
}
resource "aws_route53_record" "www_a" {
  zone_id = aws_route53_zone.site_zone.zone_id
  name    = local.www_domain_name
  type    = "A"
  alias {
    name                   = aws_s3_bucket.www_bucket.website_domain
    zone_id                = aws_s3_bucket.www_bucket.hosted_zone_id
    evaluate_target_health = false
  }
}
resource "aws_route53_record" "root_a" {
  zone_id = aws_route53_zone.site_zone.zone_id
  name    = var.domain_name
  type    = "A"
  alias {
    name                   = aws_s3_bucket.root_bucket.website_domain
    zone_id                = aws_s3_bucket.root_bucket.hosted_zone_id
    evaluate_target_health = false
  }
}

// SSL certificate
resource "aws_acm_certificate" "ssl_certificate" {
  domain_name               = var.domain_name
  subject_alternative_names = [local.www_domain_name, "*.${var.domain_name}"]
  validation_method         = "DNS"
  lifecycle {
    create_before_destroy = true
  }
  tags = local.common_tags
}
resource "aws_route53_record" "cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.ssl_certificate.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      type   = dvo.resource_record_type
      record = dvo.resource_record_value
    }
  }
  zone_id         = aws_route53_zone.site_zone.zone_id
  name            = each.value.name
  type            = each.value.type
  records         = [each.value.record]
  ttl             = 60
  allow_overwrite = true
}
resource "aws_acm_certificate_validation" "cert_validation" {
  certificate_arn         = aws_acm_certificate.ssl_certificate.arn
  validation_record_fqdns = [for record in aws_route53_record.cert_validation : record.fqdn]
}
