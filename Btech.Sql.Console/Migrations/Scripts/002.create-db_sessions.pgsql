CREATE TABLE IF NOT EXISTS "database_sessions"
(
    "user_email"             VARCHAR(128) NOT NULL,
    "access_token"      TEXT PRIMARY KEY,
    "connection_string" TEXT         NOT NULL,
    "refresh_token"     TEXT         NOT NULL,
    CONSTRAINT fk_user_session FOREIGN KEY ("user_email") REFERENCES "user_sessions" ("email") ON DELETE CASCADE
);