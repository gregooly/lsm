-- Run against MySQL database `laundry_ms` if these columns are missing (Collections detail + job-linked scans).
-- Safe to run once; remove comments if your client disallows them.

ALTER TABLE logistics_jobs
    ADD COLUMN from_location_id BIGINT UNSIGNED NULL,
    ADD COLUMN to_location_id BIGINT UNSIGNED NULL,
    ADD COLUMN reader_way_id BIGINT UNSIGNED NULL;

ALTER TABLE logistics_jobs
    ADD CONSTRAINT fk_logistics_jobs_from_location
        FOREIGN KEY (from_location_id) REFERENCES locations (id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_logistics_jobs_to_location
        FOREIGN KEY (to_location_id) REFERENCES locations (id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_logistics_jobs_reader_way
        FOREIGN KEY (reader_way_id) REFERENCES reader_ways (id) ON DELETE SET NULL;

ALTER TABLE linen_movement_events
    ADD COLUMN logistics_job_id BIGINT UNSIGNED NULL;

ALTER TABLE linen_movement_events
    ADD CONSTRAINT fk_linen_movement_events_logistics_job
        FOREIGN KEY (logistics_job_id) REFERENCES logistics_jobs (id) ON DELETE SET NULL;

CREATE INDEX ix_linen_movement_events_logistics_job_id ON linen_movement_events (logistics_job_id);
