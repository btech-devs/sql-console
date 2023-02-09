CREATE TABLE IF NOT EXISTS "user_sessions"
(
    "email"         VARCHAR(128) PRIMARY KEY,
    "access_token"  TEXT NOT NULL,
    "id_token"      TEXT NOT NULL,
    "refresh_token" TEXT not null
);