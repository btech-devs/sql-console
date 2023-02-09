# SQL Console

This application was implemented to be deployed in Google Cloud Platform as a **Cloud Run** service.

There are two ways to deploy the application:
1. build a Docker image using the `Dockerfile`
2. configure Continues Deployment from the repository

# Before creating a Cloud Run service

Create a SQL instance https://console.cloud.google.com/sql/instances (required to store user session info without storing any private data)
1. choose `PostgreSQL` database
2. choose `Development` in **Choose a configuration to start with** section (to optimize the cost)
3. choose `Lightweight` in **Machine type** section
4. choose `HDD` in **Storage type** section
5. choose `10 GB` in **Storage capacity** section
6. disable **Enable automatic storage increase** checkbox
7. enable `Private IP` in **Instance IP assignment** section
8. create a Database for the application

Create a VPC Connector https://console.cloud.google.com/networking/connectors/list (required to connect to the SQL instance)
1. choose `europe-west1` (or another one that can be used for VPC) in **Region** section
2. choose `Custom IP range` in **Subnet** section
3. set `10.34.0.0` in **IP range** input

Create a Service Account https://console.cloud.google.com/iam-admin/serviceaccounts (required to restrict user access based on IAM)
1. choose `Viewer` role for the account
2. add the account into your project **IAM** https://console.cloud.google.com/iam-admin/iam
3. create new JSON key for the account 

Generate your RSA key pair to secure connection session data
1. generate a private key `openssl genrsa -out private.pem 1024`
2. generate a public key `openssl rsa -in private.pem -pubout -out public.pem`

# Docker image

1. push a Docker image into your Google Container Registry https://cloud.google.com/container-registry/docs/pushing-and-pulling
2. create a new Cloud Run service https://console.cloud.google.com/run
3. choose your Docker image in **Deploy one revision from an existing container image** section
4. choose `europe-west1` region (or another one that can be used for VPC)
5. choose `CPU is only allocated during request processing` in **CPU allocation and pricing** section
6. set `0` in **Minimum number of instances** (to optimize the cost)
7. choose `Allow unauthenticated invocations` in **Authentication** section
8. choose your VPC Access Connector in **Networking** section
9. add the following **Environment variables**
   * `DB_HOST` - a private IP of the SQL instance
   * `DB_NAME` - the database name
   * `DB_USER` - a database user
   * `DB_PASS` - a database user password
   * `IAM_SERVICE_ACCOUNT_EMAIL` - the service account email
   * `IAM_SERVICE_ACCOUNT_CONFIG_JSON` - the JSON service account key
   * `IAM_SERVICE_GRANTED_ROLES` (optional) - `cloudsql.admin,cloudsql.client,cloudsql.editor` (IAM roles to get access to the app: `Cloud SQL Admin` / `Cloud SQL Client` / `Cloud SQL editor`)
   * `CRYPTOGRAPHY_PRIVATE_KEY` - your generated private RSA key
   * `CRYPTOGRAPHY_PUBLIC_KEY` - your generated public RSA key

[//]: # (# Continues Deployment)