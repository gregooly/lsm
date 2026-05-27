-- Logistics jobs operations upgrade.
-- Run once on MySQL 8+.

ALTER TABLE linen_movement_events
    ADD COLUMN IF NOT EXISTS logistics_job_id BIGINT UNSIGNED NULL AFTER reader_way_id;

CREATE INDEX IF NOT EXISTS ix_logistics_type_status_created
    ON logistics_jobs (job_type, job_status, created_at);

CREATE INDEX IF NOT EXISTS ix_logistics_status_created
    ON logistics_jobs (job_status, created_at);

CREATE INDEX IF NOT EXISTS ix_logistics_customer_created
    ON logistics_jobs (customer_id, created_at);

CREATE INDEX IF NOT EXISTS ix_logistics_driver_created
    ON logistics_jobs (driver_id, created_at);

CREATE INDEX IF NOT EXISTS ix_logistics_from_loc
    ON logistics_jobs (from_location_id);

CREATE INDEX IF NOT EXISTS ix_logistics_to_loc
    ON logistics_jobs (to_location_id);

CREATE INDEX IF NOT EXISTS ix_logistics_planned_end
    ON logistics_jobs (planned_end_at);

CREATE INDEX IF NOT EXISTS ix_lme_job_occurred
    ON linen_movement_events (logistics_job_id, occurred_at);
