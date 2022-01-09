/*
  Pass AWS backend configuration via CLI args e.g.

  terraform init `
    -backend-config="bucket=<s3_bucket_name>" `
    -backend-config="region=<aws_region_name>" `
    -backend-config="dynamodb_table=<dynamodb_lock_table_name>"
  
  Ensure the following environment variables are set:
  - TF_VAR_domain_name
  - TF_VAR_bungie_api_key
  - TF_VAR_destiny_membership_type
  - TF_VAR_destiny_membership_id
  - TF_VAR_destiny_character_id
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
variable "bungie_api_key" {
  type        = string
  description = "A Bungie API key retrieved from the Bungie Developer Portal."
  sensitive = true
}
variable "destiny_membership_type" {
  type        = string
  description = "The membership type of the Destiny 2 account whose activity availability will be queried."
}
variable "destiny_membership_id" {
  type        = string
  description = "The membership ID of the Destiny 2 account whose activity availability will be queried."
}
variable "destiny_character_id" {
  type        = string
  description = "The ID of the Destiny 2 character whose activity availability will be queried."
}

// Setup
provider "aws" {
  region = "us-east-1"
  default_tags {
    tags = {
      Project = var.domain_name
    }
  }
}

// SSL certificate
resource "aws_acm_certificate" "ssl_certificate" {
  domain_name               = var.domain_name
  subject_alternative_names = ["*.${var.domain_name}"]
  validation_method         = "DNS"
  lifecycle {
    create_before_destroy = true
  }
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
}
resource "aws_s3_bucket" "root_bucket" {
  bucket = local.root_s3_bucket_name
  policy = templatefile("build/templates/s3-policy.json", { bucket = local.root_s3_bucket_name })
  website {
    redirect_all_requests_to = "https://www.${var.domain_name}"
  }
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
}

// Route 53 configuration
resource "aws_route53_zone" "site_zone" {
  name = var.domain_name
  lifecycle {
    // don't destroy the hosted zone as its nameservers are referenced by the domain registrar
    prevent_destroy = true
  }
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

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep 1 dummy image"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["dummy"]
          countType     = "imageCountMoreThan"
          countNumber   = 1
        }
        action = {
          type = "expire"
        }
      },
      {
        rulePriority = 2
        description  = "Keep last 3 images"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = 3
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
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

// Create a group for local developers that is allowed to assume Lambda roles
resource "aws_iam_group" "developers" {
  name = "today-in-destiny2-developers"

  lifecycle {
    // don't destroy the group as members will be manually added to it
    prevent_destroy = true
  }
}
data "aws_iam_group" "developers" {
  group_name = aws_iam_group.developers.name
}

// Lambda function for refreshing current activities
locals {
  refresh_current_activities_lambda_name = "RefreshCurrentActivities"
}
data "aws_iam_policy_document" "refresh_current_activities_lambda_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
    principals {
      type        = "AWS"
      identifiers = [for user in data.aws_iam_group.developers.users : user.arn]
    }
  }
}
resource "aws_iam_role" "refresh_current_activities_lambda_role" {
  name               = "role.lambda.${local.refresh_current_activities_lambda_name}"
  assume_role_policy = data.aws_iam_policy_document.refresh_current_activities_lambda_assume_role.json
}
resource "aws_cloudwatch_log_group" "refresh_current_activities_lambda_logs" {
  name              = "/aws/lambda/${local.refresh_current_activities_lambda_name}"
  retention_in_days = 14
}
data "aws_iam_policy_document" "refresh_current_activities_lambda_policy" {
  statement {
    effect  = "Allow"
    actions = ["logs:CreateLogStream", "logs:PutLogEvents"]
    resources = [
      aws_cloudwatch_log_group.refresh_current_activities_lambda_logs.arn,
      "${aws_cloudwatch_log_group.refresh_current_activities_lambda_logs.arn}:*"
    ]
  }
  statement {
    effect  = "Allow"
    actions = ["s3:PutObject"]
    resources = [
      "${aws_s3_bucket.data_bucket.arn}/*"
    ]
  }
  statement {
    effect  = "Allow"
    actions = ["cloudfront:CreateInvalidation"]
    resources = [
      aws_cloudfront_distribution.data_s3_distribution.arn
    ]
  }
}
resource "aws_iam_policy" "refresh_current_activities_lambda_policy" {
  name   = "policy.lambda.${local.refresh_current_activities_lambda_name}"
  path   = "/"
  policy = data.aws_iam_policy_document.refresh_current_activities_lambda_policy.json
}
resource "aws_iam_role_policy_attachment" "refresh_current_activities_lambda_apply_permissions" {
  role       = aws_iam_role.refresh_current_activities_lambda_role.name
  policy_arn = aws_iam_policy.refresh_current_activities_lambda_policy.arn
}
resource "aws_lambda_function" "refresh_current_activities_lambda" {
  function_name = local.refresh_current_activities_lambda_name
  role          = aws_iam_role.refresh_current_activities_lambda_role.arn
  package_type  = "Image"
  image_uri     = "${aws_ecr_repository.tasks_repo.repository_url}:dummy"
  layers        = []
  image_config {
    command = ["TodayInDestiny2.Tasks::TodayInDestiny2.Tasks.LambdaEntryPoints::RefreshCurrentActivitiesHandler"]
  }
  environment {
    variables = {
      TID2_BUNGIE_API_KEY             = var.bungie_api_key
      TID2_DESTINY_MEMBERSHIP_TYPE    = var.destiny_membership_type
      TID2_DESTINY_MEMBERSHIP_ID      = var.destiny_membership_id
      TID2_DESTINY_CHARACTER_ID       = var.destiny_character_id
      TID2_DATA_S3_BUCKET_NAME        = aws_s3_bucket.data_bucket.bucket
      TID2_CLOUDFRONT_DISTRIBUTION_ID = aws_cloudfront_distribution.data_s3_distribution.id
    }
  }
  timeout = 60

  depends_on = [
    null_resource.dummy_container_image,
    aws_iam_role_policy_attachment.refresh_current_activities_lambda_apply_permissions
  ]
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
output "refresh_current_activities_lambda_function_arn" {
  value       = aws_lambda_function.refresh_current_activities_lambda.arn
  description = "The ARN of the refresh current activities Lambda function"
}
