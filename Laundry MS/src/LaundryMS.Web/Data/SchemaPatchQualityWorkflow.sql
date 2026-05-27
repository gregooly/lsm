-- Quality workflow support table and indexes.
-- Run once on MySQL 8+.

CREATE TABLE IF NOT EXISTS linen_quality_events (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  linen_item_id BIGINT UNSIGNED NOT NULL,
  event_type VARCHAR(40) NOT NULL,
  from_condition VARCHAR(24) NULL,
  to_condition VARCHAR(24) NULL,
  note VARCHAR(500) NULL,
  reported_by VARCHAR(120) NULL,
  resolved_by VARCHAR(120) NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_lqe_linen FOREIGN KEY (linen_item_id) REFERENCES linen_items (id)
);

CREATE INDEX IF NOT EXISTS ix_lqe_item_created_at
  ON linen_quality_events (linen_item_id, created_at);

CREATE INDEX IF NOT EXISTS ix_lqe_event_created_at
  ON linen_quality_events (event_type, created_at);

CREATE INDEX IF NOT EXISTS ix_linen_items_condition_active
  ON linen_items (physical_condition, is_active);

CREATE INDEX IF NOT EXISTS ix_linen_items_quality_updated
  ON linen_items (updated_at);
