-- Bootstrap for the Transactions service database.
-- Auto-executed by the Postgres container on first boot
-- (mounted into /docker-entrypoint-initdb.d/).
--
-- For production schema evolution, use EF Core Migrations
-- (see scripts/generate-migrations.sh). This script is the idempotent "v0"
-- that lets local development run without any extra tooling.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

ALTER DATABASE transactions_db SET log_min_duration_statement = '500ms';

-- ─────────────────────────── transactions ───────────────────────────
CREATE TABLE IF NOT EXISTS transactions (
    id                uuid           PRIMARY KEY,
    merchant_id       uuid           NOT NULL,
    amount            numeric(18,2)  NOT NULL CHECK (amount > 0),
    currency          char(3)        NOT NULL DEFAULT 'BRL',
    type              int            NOT NULL CHECK (type IN (1, 2)),  -- 1=Credit, 2=Debit
    occurred_on_utc   timestamptz    NOT NULL,
    description       varchar(500)   NULL
);

CREATE INDEX IF NOT EXISTS ix_transactions_merchant_date
    ON transactions (merchant_id, occurred_on_utc);

COMMENT ON TABLE  transactions IS 'Immutable record of debits and credits per merchant.';
COMMENT ON COLUMN transactions.type IS '1 = Credit, 2 = Debit';

-- ────────────────────────── outbox_messages ──────────────────────────
CREATE TABLE IF NOT EXISTS outbox_messages (
    id                 uuid          PRIMARY KEY,
    type               text          NOT NULL,
    payload            jsonb         NOT NULL,
    occurred_at_utc    timestamptz   NOT NULL,
    published_at_utc   timestamptz   NULL,
    attempts           int           NOT NULL DEFAULT 0,
    last_error         text          NULL
);

-- Partial index: the OutboxPublisher only scans pending rows
CREATE INDEX IF NOT EXISTS ix_outbox_pending
    ON outbox_messages (occurred_at_utc)
    WHERE published_at_utc IS NULL;

COMMENT ON TABLE outbox_messages IS
    'Transactional outbox. Rows are inserted in the same transaction as the aggregate and published asynchronously by the OutboxPublisher.';
