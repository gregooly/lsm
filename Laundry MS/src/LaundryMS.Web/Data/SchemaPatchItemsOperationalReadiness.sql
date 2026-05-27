-- Items operational readiness upgrade.
-- Run once on MySQL 8+.

ALTER TABLE linen_items
    ADD COLUMN last_scanned_at DATETIME(6) NULL AFTER physical_condition,
    ADD COLUMN lifecycle_state VARCHAR(24) NOT NULL DEFAULT 'active' AFTER last_scanned_at,
    ADD COLUMN deactivation_reason VARCHAR(200) NULL AFTER lifecycle_state;

UPDATE linen_items li
LEFT JOIN (
    SELECT linen_item_id, MAX(occurred_at) AS max_occurred_at
    FROM linen_movement_events
    GROUP BY linen_item_id
) ev ON ev.linen_item_id = li.id
SET li.last_scanned_at = ev.max_occurred_at
WHERE li.last_scanned_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_linen_items_active_status_location
    ON linen_items (is_active, current_process_status, current_location_id);

CREATE INDEX IF NOT EXISTS ix_linen_items_last_scanned_at
    ON linen_items (last_scanned_at);

CREATE INDEX IF NOT EXISTS ix_linen_items_owner_active
    ON linen_items (owner_customer_id, is_active);

CREATE INDEX IF NOT EXISTS ix_linen_events_item_occurred_result
    ON linen_movement_events (linen_item_id, occurred_at, processing_result);
