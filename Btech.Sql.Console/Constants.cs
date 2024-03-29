namespace Btech.Sql.Console;

public static class Constants
{
    public static class Identity
    {
        public const string GoogleIdentityAuthenticationSchemeName = "1GoogleIdentityScheme";
        public const string SessionAuthenticationSchemeName = "2SessionScheme";

        public const string FullAuthenticationPolicyName = "FullPolicy";
        public const string GoogleIdentityAuthorizationPolicyName = "GoogleIdentity";
        public const string SessionAuthorizationPolicyName = "Session";

        public const string SecretManagerSessionDataVersionId = "1";

        public const string SessionDataItemName = "SessionData";

        public static class CookieNames
        {
            public const string RefreshTokenCookieName = "google_refresh_token";
        }

        public static class HeaderNames
        {
            public static class Request
            {
                public const string IdTokenHeaderName = "sql-console-id-token";
                public const string DbSessionTokenHeaderName = "sql-console-session-token";
                public const string DbRefreshTokenHeaderName = "sql-console-refresh-token";
            }

            public static class Response
            {
                public const string RefreshedIdTokenHeaderName = "refreshed-id-token";
                public const string RefreshedSessionTokenHeaderName = "refreshed-session-token";
                public const string RefreshedRefreshTokenHeaderName = "refreshed-refresh-token";
                public const string IdentityErrorHeaderName = "identity-error";
            }
        }

        public static class ClaimTypes
        {
            public const string InstanceType = "instance_type";
            public const string Host = "host";
            public const string Email = "email";
            public const string EmailVerified = "email_verified";
            public const string Picture = "picture";
            public const string ConnectionString = "connection_string";
            public const string Expiration = "exp";
            public const string Role = "role";
        }

        public static class IamServiceRoleNames
        {
            public const string CloudSqlAdmin = "roles/cloudsql.admin";
            public const string Owner = "roles/owner";
            public const string CloudSqlEditor = "roles/cloudsql.editor";
            public const string Editor = "roles/editor";
            public const string CloudSqlClient = "roles/cloudsql.client";
        }

        public static class StaticConnectionEnvironmentVariables
        {
            public const string Host = "STATIC_HOST";
            public const string Port = "STATIC_PORT";
            public const string User = "STATIC_USER";
            public const string Password = "STATIC_PASSWORD";
            public const string InstanceType = "STATIC_INSTANCE_TYPE";
        }
    }

    public const string ClientSecretEnvironmentVariableName = "CLIENT_SECRET";
    public const string ClientIdEnvironmentVariableName = "CLIENT_ID";
    public const string ProjectIdEnvironmentVariableName = "PROJECT_ID";
    public const string IamServiceAccountConfigJsonEnvironmentVariableName = "IAM_SERVICE_ACCOUNT_CONFIG_JSON";
    public const string IamServiceAccountEmailEnvironmentVariableName = "IAM_SERVICE_ACCOUNT_EMAIL";
    public const string IamServiceAccountPrivateKeyEnvironmentVariableName = "IAM_SERVICE_ACCOUNT_PRIVATE_KEY";
    public const string CryptographyPublicKeyEnvironmentVariableName = "CRYPTOGRAPHY_PUBLIC_KEY";
    public const string CryptographyPrivateKeyEnvironmentVariableName = "CRYPTOGRAPHY_PRIVATE_KEY";
    public const string EnvironmentEnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";
    public const string IamServiceGrantedRolesEnvironmentVariableName = "IAM_SERVICE_GRANTED_ROLES";
    public const string SecretManagerServiceAccountConfigJsonEnvironmentVariableName = "SECRET_MANAGER_SERVICE_ACCOUNT_CONFIG_JSON";

    public const ushort HostMaxLength = 263;
    public const ushort PortMinValue = 1;
    public const ushort PortMaxValue = 65535;
    public const ushort PasswordMaxLength = 90;
    public const ushort UsernameMaxLength = 63;

    public static class ValidationErrorMessageTemplates
    {
        // 0 - property name
        // 1..n - arguments
        public const string Required = "{0} is required";
        public const string MaxLength = "{0} can not be longer than {1} characters";
        public const string Range = "{0} must be between {1} and {2}";
    }

    public const string AuditApi = "https://btech-sql-console-audit-jrqf7pzn4q-ew.a.run.app/api";
}