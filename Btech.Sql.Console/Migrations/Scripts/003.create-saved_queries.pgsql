CREATE TABLE IF NOT EXISTS "saved_queries"
(
    "id"            BIGSERIAL   PRIMARY KEY,
    "user_email"    VARCHAR(128) NOT NULL,
    "query_name"    VARCHAR(128) NOT NULL,
    "query"         TEXT         NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX:saved_queries(user_email)" ON saved_queries (user_email);