CREATE TABLE IF NOT EXISTS reports (
    Id        TEXT PRIMARY KEY,
    Text      TEXT NOT NULL,
    Location  TEXT NOT NULL,
    Status    TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS outbox_messages (
    Id            TEXT   PRIMARY KEY,
    CorrelationId TEXT   NOT NULL,
    Type          TEXT   NOT NULL,
    Payload       TEXT   NOT NULL,
    Attempts      NUMBER NOT NULL DEFAULT 0,
    Errors        TEXT   NOT NULL DEFAULT '[]',
    CreatedAt     TEXT   NOT NULL,
    CompletedAt   TEXT   NULL
);