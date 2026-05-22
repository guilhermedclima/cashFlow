-- Bootstrap for the Reporting service database.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

ALTER DATABASE reporting_db SET log_min_duration_statement = '500ms';

-- ─────────────────────────── daily_balances ───────────────────────────
CREATE TABLE IF NOT EXISTS daily_balances (
    merchant_id         uuid           NOT NULL,
    date                date           NOT NULL,
    total_credits       numeric(18,2)  NOT NULL DEFAULT 0,
    total_debits        numeric(18,2)  NOT NULL DEFAULT 0,
    transaction_count   int            NOT NULL DEFAULT 0,
    updated_at_utc      timestamptz    NOT NULL DEFAULT now(),
    CONSTRAINT pk_daily_balances PRIMARY KEY (merchant_id, date)
);

COMMENT ON TABLE daily_balances IS
    'Materialized read model. Updated by consuming TransactionRegisteredEvent.';

-- ─────────────────────── processed_transactions ───────────────────────
CREATE TABLE IF NOT EXISTS processed_transactions (
    transaction_id      uuid         PRIMARY KEY,
    processed_at_utc    timestamptz  NOT NULL DEFAULT now()
);

COMMENT ON TABLE processed_transactions IS
    'Idempotency table — primary key on transaction_id prevents reapplying the same event.';
