-- Handheld R2 / app device id string (set when driver registers on mobile app).
-- Run once on MySQL 8+.

ALTER TABLE drivers
    ADD COLUMN handheld_device_id VARCHAR(120) NULL AFTER vehicle_registration_no;

CREATE UNIQUE INDEX ix_drivers_handheld_device_id ON drivers (handheld_device_id);
