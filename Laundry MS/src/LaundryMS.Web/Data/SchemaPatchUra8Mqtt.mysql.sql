-- URA8 fixed readers: MQTT credentials on readers + antenna-mapped scan routes.
-- Run against laundry_ms after backups.

ALTER TABLE readers
    ADD COLUMN mqtt_username VARCHAR(64) NULL COMMENT 'EMQX HTTP auth username' AFTER maintenance_note,
    ADD COLUMN mqtt_password_hash VARCHAR(255) NULL COMMENT 'BCrypt hash for MQTT password' AFTER mqtt_username;

CREATE UNIQUE INDEX ux_readers_mqtt_username ON readers (mqtt_username);

ALTER TABLE reader_ways
    ADD COLUMN antenna_index INT NOT NULL DEFAULT 0 COMMENT '0=fallback; 1-8=URA8 antenna port' AFTER target_process_status;

-- Existing installs: first route per reader stays fallback (0); additional routes get 1..n-1.
UPDATE reader_ways rw
INNER JOIN (
    SELECT id,
           ROW_NUMBER() OVER (PARTITION BY reader_id ORDER BY id) AS rn
    FROM reader_ways
) x ON rw.id = x.id
SET rw.antenna_index = CASE WHEN x.rn = 1 THEN 0 ELSE x.rn - 1 END;

CREATE UNIQUE INDEX uq_reader_ways_antenna_per_reader ON reader_ways (reader_id, antenna_index);
