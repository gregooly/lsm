-- Linen Assignment operational upgrade.
-- Run once on MySQL 8+.

CREATE TABLE IF NOT EXISTS linen_assignment_events (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  linen_item_id BIGINT UNSIGNED NOT NULL,
  changed_at DATETIME(6) NOT NULL,
  changed_by VARCHAR(120) NULL,
  change_source VARCHAR(60) NOT NULL,
  from_json LONGTEXT NULL,
  to_json LONGTEXT NULL,
  note VARCHAR(300) NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_lae_linen FOREIGN KEY (linen_item_id) REFERENCES linen_items (id)
);

CREATE INDEX IF NOT EXISTS ix_lae_item_changed_at
  ON linen_assignment_events (linen_item_id, changed_at);

CREATE INDEX IF NOT EXISTS ix_lae_changed_at
  ON linen_assignment_events (changed_at);

CREATE INDEX IF NOT EXISTS ix_linen_items_owner_assignment
  ON linen_items (owner_customer_id, default_assignment_type);

CREATE INDEX IF NOT EXISTS ix_linen_items_status_assignment
  ON linen_items (current_process_status, default_assignment_type);
