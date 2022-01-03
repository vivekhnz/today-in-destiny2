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
    null = {
      source  = "hashicorp/null"
      version = "3.1.0"
    }
  }

  backend "s3" {
    key     = "terraform.tfstate"
    encrypt = true
  }
}

// Inputs
variable "domain_name" {
  type        = string
  description = "The domain name for the website excluding the 'www.' prefix."
}

// Setup
locals {
  common_tags = {
    Project = var.domain_name
  }
}

provider "aws" {
  region = "us-east-1"
}

// SSL certificate
resource "aws_acm_certificate" "ssl_certificate" {
  domain_name               = var.domain_name
  subject_alternative_names = ["*.${var.domain_name}"]
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

// Static website S3 buckets
locals {
  www_s3_bucket_name  = "www.${var.domain_name}"
  root_s3_bucket_name = var.domain_name
}
resource "aws_s3_bucket" "www_bucket" {
  bucket = local.www_s3_bucket_name
  policy = templatefile("build/templates/s3-policy.json", { bucket = local.www_s3_bucket_name })
  website {
    index_document = "index.html"
  }
  cors_rule {
    allowed_headers = ["Authorization", "Content-Length"]
    allowed_methods = ["GET", "POST"]
    allowed_origins = ["https://www.${var.domain_name}"]
    max_age_seconds = 3000
  }
  tags = local.common_tags
}
resource "aws_s3_bucket" "root_bucket" {
  bucket = local.root_s3_bucket_name
  policy = templatefile("build/templates/s3-policy.json", { bucket = local.root_s3_bucket_name })
  website {
    redirect_all_requests_to = "https://www.${var.domain_name}"
  }
  tags = local.common_tags
}

// S3 bucket to store data
locals {
  data_s3_bucket_name = "data.${var.domain_name}"
}
resource "aws_s3_bucket" "data_bucket" {
  bucket = local.data_s3_bucket_name
  policy = templatefile("build/templates/s3-policy.json", { bucket = local.data_s3_bucket_name })
  website {
    index_document = "index.html"
  }
  cors_rule {
    allowed_headers = ["Authorization", "Content-Length"]
    allowed_methods = ["GET", "POST"]
    allowed_origins = ["https://www.${var.domain_name}"]
    max_age_seconds = 3000
  }
  tags = local.common_tags
}
resource "aws_s3_bucket_object" "data_index_html" {
  bucket       = local.data_s3_bucket_name
  key          = "index.html"
  source       = "data/index.html"
  content_type = "text/html"
  etag         = filemd5("data/index.html")

  depends_on = [aws_s3_bucket.data_bucket]
}
resource "aws_s3_bucket_object" "data_d2_folder" {
  bucket       = local.data_s3_bucket_name
  key          = "d2/"
  content_type = "application/x-directory"
  lifecycle {
    // don't destroy the folder as other services will upload files into it
    prevent_destroy = true
  }

  depends_on = [aws_s3_bucket.data_bucket]
}
resource "aws_s3_bucket_object" "data_d2_today_json" {
  bucket       = local.data_s3_bucket_name
  key          = "d2/today.json"
  source       = "data/d2/today.json"
  content_type = "application/json"
  etag         = filemd5("data/d2/today.json")

  depends_on = [aws_s3_bucket_object.data_d2_folder]
}

// CloudFront distributions
locals {
  www_s3_origin_id  = "S3-${local.www_s3_bucket_name}"
  root_s3_origin_id = "S3-${local.root_s3_bucket_name}"
  data_s3_origin_id = "S3-${local.data_s3_bucket_name}"
}
resource "aws_cloudfront_distribution" "www_s3_distribution" {
  origin {
    domain_name = aws_s3_bucket.www_bucket.website_endpoint
    origin_id   = local.www_s3_origin_id
    custom_origin_config {
      http_port              = "80"
      https_port             = "443"
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1", "TLSv1.1", "TLSv1.2"]
    }
  }
  aliases = ["www.${var.domain_name}"]

  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = local.www_s3_origin_id
    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
      headers = [
        "Origin",
        "Access-Control-Request-Headers",
        "Access-Control-Request-Method"
      ]
    }
    viewer_protocol_policy = "redirect-to-https"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.ssl_certificate.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2019"
  }

  tags = local.common_tags
}
resource "aws_cloudfront_distribution" "root_s3_distribution" {
  origin {
    domain_name = aws_s3_bucket.root_bucket.website_endpoint
    origin_id   = local.root_s3_origin_id
    custom_origin_config {
      http_port              = "80"
      https_port             = "443"
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1", "TLSv1.1", "TLSv1.2"]
    }
  }
  aliases = [var.domain_name]

  enabled         = true
  is_ipv6_enabled = true

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = local.root_s3_origin_id
    forwarded_values {
      query_string = true
      cookies {
        forward = "none"
      }
      headers = [
        "Origin",
        "Access-Control-Request-Headers",
        "Access-Control-Request-Method"
      ]
    }
    viewer_protocol_policy = "allow-all"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.ssl_certificate.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2019"
  }

  tags = local.common_tags
}
resource "aws_cloudfront_distribution" "data_s3_distribution" {
  origin {
    domain_name = aws_s3_bucket.data_bucket.website_endpoint
    origin_id   = local.data_s3_origin_id
    custom_origin_config {
      http_port              = "80"
      https_port             = "443"
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1", "TLSv1.1", "TLSv1.2"]
    }
  }
  aliases = ["data.${var.domain_name}"]

  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = local.data_s3_origin_id
    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
      headers = [
        "Origin",
        "Access-Control-Request-Headers",
        "Access-Control-Request-Method"
      ]
    }
    viewer_protocol_policy = "redirect-to-https"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.ssl_certificate.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2019"
  }

  tags = local.common_tags
}

// Route 53 configuration
resource "aws_route53_zone" "site_zone" {
  name = var.domain_name
  lifecycle {
    // don't destroy the hosted zone as its nameservers are referenced by the domain registrar
    prevent_destroy = true
  }
  tags = local.common_tags
}
resource "aws_route53_record" "www_a" {
  zone_id = aws_route53_zone.site_zone.zone_id
  name    = "www.${var.domain_name}"
  type    = "A"
  alias {
    name                   = aws_cloudfront_distribution.www_s3_distribution.domain_name
    zone_id                = aws_cloudfront_distribution.www_s3_distribution.hosted_zone_id
    evaluate_target_health = false
  }
}
resource "aws_route53_record" "root_a" {
  zone_id = aws_route53_zone.site_zone.zone_id
  name    = var.domain_name
  type    = "A"
  alias {
    name                   = aws_cloudfront_distribution.root_s3_distribution.domain_name
    zone_id                = aws_cloudfront_distribution.root_s3_distribution.hosted_zone_id
    evaluate_target_health = false
  }
}
resource "aws_route53_record" "data_a" {
  zone_id = aws_route53_zone.site_zone.zone_id
  name    = "data.${var.domain_name}"
  type    = "A"
  alias {
    name                   = aws_cloudfront_distribution.data_s3_distribution.domain_name
    zone_id                = aws_cloudfront_distribution.data_s3_distribution.hosted_zone_id
    evaluate_target_health = false
  }
}

// Private ECR repository to host container images
resource "aws_ecr_repository" "tasks_repo" {
  name                 = "today-in-destiny2-tasks"
  image_tag_mutability = "MUTABLE"
  image_scanning_configuration {
    scan_on_push = true
  }
}
resource "aws_ecr_lifecycle_policy" "tasks_repo_policy" {
  repository = aws_ecr_repository.tasks_repo.name

  policy = <<EOF
{
    "rules": [
        {
            "rulePriority": 1,
            "description": "Keep last 2 images",
            "selection": {
                "tagStatus": "any",
                "countType": "imageCountMoreThan",
                "countNumber": 2
            },
            "action": {
                "type": "expire"
            }
        }
    ]
}
EOF
}

// Push a dummy Docker image into ECR that our Lambda functions can use
// We'll swap out the image URI later on as part of deployment
resource "null_resource" "dummy_container_image" {
  provisioner "local-exec" {
    interpreter = [
      "pwsh",
      "-Command"
    ]
    command = <<EOF
./build/push-dummy-image.ps1 `
  -RepoName ${aws_ecr_repository.tasks_repo.name} `
  -RepoUri ${aws_ecr_repository.tasks_repo.repository_url}
EOF
  }
}

// Lambda function for testing purposes
resource "aws_iam_role" "test_lambda_role" {
  name = "role.TestLambda"

  assume_role_policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Action": "sts:AssumeRole",
      "Principal": {
        "Service": "lambda.amazonaws.com"
      },
      "Effect": "Allow",
      "Sid": ""
    }
  ]
}
EOF
}
resource "aws_lambda_function" "test_lambda" {
  function_name = "TestLambda"
  role          = aws_iam_role.test_lambda_role.arn
  package_type  = "Image"
  image_uri     = "${aws_ecr_repository.tasks_repo.repository_url}:dummy"
  layers        = []
  image_config {
    entry_point = ["TodayInDestiny2.Tasks::TodayInDestiny2.Tasks.Function::FunctionHandler"]
  }

  tags       = local.common_tags
  depends_on = [null_resource.dummy_container_image]

  lifecycle {
    // the deploy process will update the image URI so don't let Terraform reset it back to the dummy image
    ignore_changes = [image_uri]
  }
}

// Outputs
output "website_s3_uri" {
  value       = "s3://${local.www_s3_bucket_name}"
  description = "The S3 URI to sync static website content to"
}
output "website_cloudfront_distribution_id" {
  value       = aws_cloudfront_distribution.www_s3_distribution.id
  description = "The distribution ID of the CloudFront distribution to invalidate after updating static website content"
}
output "data_source_uri" {
  value       = "https://data.${var.domain_name}"
  description = "The URI of the website that hosts JSON data files"
}
output "tasks_container_repo_uri" {
  value       = aws_ecr_repository.tasks_repo.repository_url
  description = "The URI of the repository that hosts container images for tasks"
}
output "test_lambda_function_arn" {
  value       = aws_lambda_function.test_lambda.arn
  description = "The ARN of the test lambda function"
}
