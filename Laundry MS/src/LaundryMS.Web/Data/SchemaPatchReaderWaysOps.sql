-- Scan routes (reader_ways) operations upgrade.
-- Run once on MySQL 8+.

-- Align with app: optional from/to endpoints.
ALTER TABLE reader_ways
    MODIFY COLUMN from_location_id BIGINT UNSIGNED NULL,
    MODIFY COLUMN to_location_id BIGINT UNSIGNED NULL;

CREATE TABLE IF NOT EXISTS reader_way_events (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  reader_way_id BIGINT UNSIGNED NOT NULL,
  event_type VARCHAR(40) NOT NULL,
  note VARCHAR(400) NULL,
  changed_by VARCHAR(120) NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_reader_way_events_way FOREIGN KEY (reader_way_id) REFERENCES reader_ways (id)
);

CREATE INDEX IF NOT EXISTS ix_reader_way_events_way_created_at
    ON reader_way_events (reader_way_id, created_at);

CREATE INDEX IF NOT EXISTS ix_reader_ways_reader
    ON reader_ways (reader_id);

CREATE INDEX IF NOT EXISTS ix_reader_ways_purpose_active
    ON reader_ways (business_purpose_key, is_active);

CREATE INDEX IF NOT EXISTS ix_reader_ways_from_loc
    ON reader_ways (from_location_id);

CREATE INDEX IF NOT EXISTS ix_reader_ways_to_loc
    ON reader_ways (to_location_id);

CREATE INDEX IF NOT EXISTS ix_lme_way_occurred
    ON linen_movement_events (reader_way_id, occurred_at);
