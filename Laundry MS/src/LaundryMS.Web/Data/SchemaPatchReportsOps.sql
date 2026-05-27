-- Reports operations upgrade.
-- Run once on MySQL 8+.

CREATE INDEX IF NOT EXISTS ix_linen_items_owner_status
    ON linen_items (owner_customer_id, current_process_status);

CREATE INDEX IF NOT EXISTS ix_linen_items_location_status
    ON linen_items (current_location_id, current_process_status);

CREATE INDEX IF NOT EXISTS ix_linen_items_condition_updated
    ON linen_items (physical_condition, updated_at);

CREATE INDEX IF NOT EXISTS ix_linen_items_updated
    ON linen_items (updated_at);

CREATE INDEX IF NOT EXISTS ix_lme_occurred
    ON linen_movement_events (occurred_at);

CREATE INDEX IF NOT EXISTS ix_lme_processing_occurred
    ON linen_movement_events (processing_result, occurred_at);

CREATE INDEX IF NOT EXISTS ix_lme_reader_occurred
    ON linen_movement_events (reader_id, occurred_at);

CREATE INDEX IF NOT EXISTS ix_lme_route_occurred
    ON linen_movement_events (reader_way_id, occurred_at);

CREATE INDEX IF NOT EXISTS ix_lme_job_occurred_report
    ON linen_movement_events (logistics_job_id, occurred_at);

CREATE INDEX IF NOT EXISTS ix_logistics_status_planned_end
    ON logistics_jobs (job_status, planned_end_at);

CREATE INDEX IF NOT EXISTS ix_logistics_updated_at
    ON logistics_jobs (updated_at);
