-- Scan history operations upgrade.
-- Run once on MySQL 8+.

CREATE INDEX IF NOT EXISTS ix_lme_occurred_at
    ON linen_movement_events (occurred_at);

CREATE INDEX IF NOT EXISTS ix_lme_reader_occurred
    ON linen_movement_events (reader_id, occurred_at);

CREATE INDEX IF NOT EXISTS ix_lme_result_occurred
    ON linen_movement_events (processing_result, occurred_at);

CREATE INDEX IF NOT EXISTS ix_lme_idempotency
    ON linen_movement_events (idempotency_key);

CREATE INDEX IF NOT EXISTS ix_lme_received_at
    ON linen_movement_events (received_at_server);
