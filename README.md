# SQL Console

This application was implemented to be deployed in Google Cloud Platform as a **Cloud Run** service to connect to GCP SQL and AlloyDB instances.

# Before creating a Cloud Run service

Create API client credentials (required for Google authorization)
1. create OAuth consent screen https://console.cloud.google.com/apis/credentials/consent
2. create `OAuth client ID` credentials https://console.cloud.google.com/apis/credentials
   - choose `Web application` in **Application type** section
   - add `[your_cloud_run_service_url]/google-auth` into **Authorized redirect URIs** section

# Connection types

The service supports two types of GCP SQL connections:
1. via an instance private IP through a VPC (file `deploy-vpc.tf`)
2. via an SQL instance connection name (file `deploy-sql.tf`)

# Environment variables

This service supports the following **Environment variables** (are configured by default in `main.tf`)`:
* `CLIENT_ID` - your OAuth client ID (https://console.cloud.google.com/apis/credentials)
* `CLIENT_SECRET` - your OAuth client secret (https://console.cloud.google.com/apis/credentials)
* `IAM_SERVICE_ACCOUNT_EMAIL` - the service account email (https://console.cloud.google.com/iam-admin/serviceaccounts)
* `IAM_SERVICE_ACCOUNT_CONFIG_JSON` - the JSON service account key (https://console.cloud.google.com/iam-admin/serviceaccounts)
* `IAM_SERVICE_GRANTED_ROLES` (optional) - `cloudsql.admin,owner,cloudsql.editor,editor,cloudsql.client` (IAM roles to get access to the app: `Cloud SQL Admin` / `Owner` / `Cloud SQL Editor` / `Editor` / `Cloud SQL Client`)
* `SECRET_MANAGER_SERVICE_ACCOUNT_CONFIG_JSON` - the JSON service account key (https://console.cloud.google.com/iam-admin/serviceaccounts)
* `CRYPTOGRAPHY_PRIVATE_KEY` - your generated private RSA key (`sql-console-private.pem` file)
* `CRYPTOGRAPHY_PUBLIC_KEY` - your generated public RSA key (`sql-console-public.pem` file)

Also this service can be used for a single static connection without saving session data. The following **Environment variables** must be configured instead of `SECRET_MANAGER_SERVICE_ACCOUNT_CONFIG_JSON`:
* `STATIC_HOST` - a private IP of an SQL instance
* `STATIC_PORT` - an SQL instance port
* `STATIC_USER` - an SQL username
* `STATIC_PASSWORD` - a user password
* `STATIC_INSTANCE_TYPE` - an instance type
   * `PgSql` for PostgreSQL
   * `MsSql` for Microsoft SQL Server

1. file `deploy-static-vpc.tf` to use an instance private IP through a VPC
2. file `deploy-static-sql.tf` to use an SQL instance connection name