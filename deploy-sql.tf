## initialize variables
#
#variable "gcp_project" {
#  type        = string
#  description = "GCP project ID"
#}
#
#variable "gcp_region" {
#  type        = string
#  description = "GCP region (e.g. europe-west1)"
#}
#
#variable "gcp_oauth_client_id" {
#  type        = string
#  description = "GCP OAuth Client ID"
#}
#
#variable "gcp_oauth_client_secret" {
#  type        = string
#  description = "GCP OAuth Client secret"
#}
#
## add an SQL instance connection (to connect via a connection name)
#
#variable "gcp_sql_connection_name" {
#  type        = string
#  description = "GCP SQL instance connection name"
#}
#
## create a service account
#
#resource "google_service_account" "service_account" {
#  project      = var.gcp_project
#  account_id   = "sql-console-sa"
#  display_name = "SQL Console Service Account"
#}
#
#resource "google_project_iam_member" "sa_role_sm" {
#  project = var.gcp_project
#  role    = "roles/secretmanager.admin"
#  member  = "serviceAccount:${google_service_account.service_account.email}"
#}
#
#resource "google_project_iam_member" "sa_role_viewer" {
#  project = var.gcp_project
#  role    = "roles/viewer"
#  member  = "serviceAccount:${google_service_account.service_account.email}"
#}
#
#resource "google_service_account_key" "sa_key" {
#  service_account_id = google_service_account.service_account.name
#  public_key_type    = "TYPE_X509_PEM_FILE"
#}
#
#resource "local_file" "sa_key_file" {
#  filename  = "./sql-console-sa.json"
#  content   = base64decode(google_service_account_key.sa_key.private_key)
#}
#
## create a Docker container image
#
#resource "null_resource" "create_docker_repository" {
#  provisioner "local-exec" {
#    command = "yes | gcloud artifacts repositories create sql-console-repo --repository-format=docker --location=${var.gcp_region} --description=\"SQL Console repository\" --project=${var.gcp_project}"
#  }
#}
#
#resource "null_resource" "build_docker_image" {
#  provisioner "local-exec" {
#    command = "yes | gcloud builds submit --region=${var.gcp_region} --tag ${var.gcp_region}-docker.pkg.dev/${var.gcp_project}/sql-console-repo/sql-console --project=${var.gcp_project}"
#  }
#}
#
## enable Cloud Run Admin API
#
#resource "google_project_service" "cloud_run_api" {
#  project = var.gcp_project
#  service = "run.googleapis.com"
#}
#
## create a Cloud Run service
#
#resource "google_cloud_run_v2_service" "sql_console_service" {
#  project   = var.gcp_project
#  name      = "sql-console"
#  location  = var.gcp_region
#  ingress   = "INGRESS_TRAFFIC_ALL"
#
#  template {
#    containers {
#      image = "${var.gcp_region}-docker.pkg.dev/${var.gcp_project}/sql-console-repo/sql-console"
#
#      env {
#        name  = "CLIENT_ID"
#        value = var.gcp_oauth_client_id
#      }
#
#      env {
#        name  = "CLIENT_SECRET"
#        value = var.gcp_oauth_client_secret
#      }
#
#      env {
#        name  = "IAM_SERVICE_ACCOUNT_EMAIL"
#        value = google_service_account.service_account.email
#      }
#
#      env {
#        name  = "IAM_SERVICE_ACCOUNT_CONFIG_JSON"
#        value = local_file.sa_key_file.content
#      }
#
#      env {
#        name  = "IAM_SERVICE_GRANTED_ROLES"
#        value = "cloudsql.admin,cloudsql.editor,cloudsql.client,owner,editor"
#      }
#
#      env {
#        name  = "SECRET_MANAGER_SERVICE_ACCOUNT_CONFIG_JSON"
#        value = local_file.sa_key_file.content
#      }
#
#      env {
#        name  = "CRYPTOGRAPHY_PRIVATE_KEY"
#        value = file("${path.module}/sql-console-private.pem")
#      }
#
#      env {
#        name  = "CRYPTOGRAPHY_PUBLIC_KEY"
#        value = file("${path.module}/sql-console-public.pem")
#      }
#
#      volume_mounts {
#        name        = "cloudsql"
#        mount_path  = "/cloudsql"
#      }
#    }
#
#    scaling {
#      min_instance_count = 0
#      max_instance_count = 10
#    }
#
#    volumes {
#      name = "cloudsql"
#      cloud_sql_instance {
#        instances = [var.gcp_sql_connection_name]
#      }
#    }
#  }
#
#  depends_on = [
#    google_project_service.cloud_run_api
#  ]
#}
#
#resource "google_cloud_run_service_iam_binding" "allow_unauthenticated" {
#  location  = var.gcp_region
#  project   = var.gcp_project
#  service   = "sql-console"
#  role      = "roles/run.invoker"
#  members   = [
#    "allUsers"
#  ]
#}
#
## enable Cloud Resource Manager API
#
#resource "google_project_service" "crm_api" {
#  project = var.gcp_project
#  service = "cloudresourcemanager.googleapis.com"
#}
#
## enable Secret Manager API
#
#resource "google_project_service" "secret_manager_api" {
#  project = var.gcp_project
#  service = "secretmanager.googleapis.com"
#}
#
#resource "null_resource" "output_url" {
#  provisioner "local-exec" {
#    command = "echo \"YOU NEED TO ADD '${google_cloud_run_v2_service.sql_console_service.uri}/google-auth' INTO YOUR Client ID Authorized redirect URIs\""
#  }
#}