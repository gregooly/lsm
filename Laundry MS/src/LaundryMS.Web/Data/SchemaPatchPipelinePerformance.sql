-- Pipeline query hardening for larger datasets.
-- Safe to run once in MySQL 8+.

CREATE INDEX IF NOT EXISTS ix_linen_items_active_status
    ON linen_items (is_active, current_process_status);

CREATE INDEX IF NOT EXISTS ix_linen_items_active_location
    ON linen_items (is_active, current_location_id);

CREATE INDEX IF NOT EXISTS ix_linen_movement_events_occurred_at
    ON linen_movement_events (occurred_at);

CREATE INDEX IF NOT EXISTS ix_linen_movement_events_item_occurred_at
    ON linen_movement_events (linen_item_id, occurred_at);