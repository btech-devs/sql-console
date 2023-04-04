# SQL Console

This application was implemented to be deployed in Google Cloud Platform as a **Cloud Run** service.

# Before creating a Cloud Run service

Create API client keys (required for Google authorization)
1. create OAuth consent screen https://console.cloud.google.com/apis/credentials/consent
2. create `OAuth client ID` credentials https://console.cloud.google.com/apis/credentials
   - choose `Web application` in **Application type** section
   - add `[your_cloud_run_service_url]/google-auth` into **Authorized redirect URIs** section

Create a VPC Connector https://console.cloud.google.com/networking/connectors/list (required to connect to the SQL instance)
1. choose `europe-west1` (or another one that can be used for VPC) in **Region** section
2. choose `Custom IP range` in **Subnet** section
3. set `10.34.0.0` in **IP range** input

Create a Service Account https://console.cloud.google.com/iam-admin/serviceaccounts (required to restrict user access based on IAM and to store session data in Secret Manager)
1. choose `Viewer` and `Secret Manager Admin` roles for the account
2. add the account into your project **IAM** https://console.cloud.google.com/iam-admin/iam
3. create new JSON key for the account

Generate your RSA key pair to secure connection session data
1. generate a private key `openssl genrsa -out sql-console-private.pem 1024`
2. generate a public key `openssl rsa -in sql-console-private.pem -pubout -out sql-console-public.pem`

Create a Docker container image
1. open Cloud Shell https://console.cloud.google.com/cloudshelleditor
2. execute the following commands (full manual https://cloud.google.com/build/docs/build-push-docker-image)
   - pull the repository `git clone https://github.com/btech-devs/sql-console.git`
   - move to the directory `cd sql-console`
   - create a cloud repository `gcloud artifacts repositories create sql-console-repo --repository-format=docker --location=europe-west1 --description="SQL Console repository" --project=[project_id]` (insert your `project_id` value)
   - build a container image `gcloud builds submit --region=europe-west1 --tag europe-west1-docker.pkg.dev/[project_id]/sql-console-repo/sql-console:tag1 --project=[project_id]` (insert your `project_id` value)

# Docker image

[//]: # (1. push a Docker image into your Google Container Registry https://cloud.google.com/container-registry/docs/pushing-and-pulling)
1. create a new Cloud Run service https://console.cloud.google.com/run
2. choose your Docker image in **Deploy one revision from an existing container image** section
3. choose `europe-west1` region (or another one that can be used for VPC)
4. choose `CPU is only allocated during request processing` in **CPU allocation and pricing** section
5. set `0` in **Minimum number of instances** (to optimize the cost)
6. choose `Allow unauthenticated invocations` in **Authentication** section
7. choose your VPC Access Connector in **Networking** section
8. add the following **Environment variables**
   * `CLIENT_ID` - your OAuth client ID (https://console.cloud.google.com/apis/credentials)
   * `CLIENT_SECRET` - your OAuth client secret (https://console.cloud.google.com/apis/credentials)
   * `IAM_SERVICE_ACCOUNT_EMAIL` - the service account email (https://console.cloud.google.com/iam-admin/serviceaccounts)
   * `IAM_SERVICE_ACCOUNT_CONFIG_JSON` - the JSON service account key (https://console.cloud.google.com/iam-admin/serviceaccounts)
   * `IAM_SERVICE_GRANTED_ROLES` (optional) - `cloudsql.admin,cloudsql.editor,cloudsql.client` (IAM roles to get access to the app: `Cloud SQL Admin` / `Cloud SQL Editor` / `Cloud SQL Client`)
   * `SECRET_MANAGER_SERVICE_ACCOUNT_CONFIG_JSON` - the JSON service account key (https://console.cloud.google.com/iam-admin/serviceaccounts)
   * `CRYPTOGRAPHY_PRIVATE_KEY` - your generated private RSA key (`sql-console-private.pem` file)
   * `CRYPTOGRAPHY_PUBLIC_KEY` - your generated public RSA key (`sql-console-public.pem` file)

Also this service can be used for a single static connection without saving session data. All Secret Manager configurations can be ignored in this case. The following **Environment variables** must be configured:
* `STATIC_HOST` - a private IP of an SQL instance
* `STATIC_PORT` - an SQL instance port
* `STATIC_USER` - an SQL username
* `STATIC_PASSWORD` - a user password
* `STATIC_INSTANCE_TYPE` - an instance type
   * `PgSql` for PostgreSQL
   * `MsSql` for Microsoft SQL Server