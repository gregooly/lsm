-- Readers operations upgrade.
-- Run once on MySQL 8+.

ALTER TABLE readers
    ADD COLUMN installed_at DATETIME(6) NULL AFTER reader_category,
    ADD COLUMN last_heartbeat_at DATETIME(6) NULL AFTER installed_at,
    ADD COLUMN maintenance_note VARCHAR(300) NULL AFTER last_heartbeat_at;

CREATE TABLE IF NOT EXISTS reader_events (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  reader_id BIGINT UNSIGNED NOT NULL,
  event_type VARCHAR(40) NOT NULL,
  note VARCHAR(400) NULL,
  changed_by VARCHAR(120) NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_reader_events_reader FOREIGN KEY (reader_id) REFERENCES readers (id)
);

CREATE INDEX IF NOT EXISTS ix_reader_events_reader_created_at
  ON reader_events (reader_id, created_at);

CREATE INDEX IF NOT EXISTS ix_readers_category_active
  ON readers (reader_category, is_active);

CREATE INDEX IF NOT EXISTS ix_readers_device_identifier
  ON readers (device_identifier);
