-- Adds tenant key column (customer_id) to core operational tables.
-- Notes:
-- 1) This script assumes MySQL 8+ for ADD COLUMN IF NOT EXISTS.
-- 2) Keep owner_customer_id in linen_items; this adds customer_id as tenant scope key.

ALTER TABLE `customers`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_customers_customer_id` ON `customers` (`customer_id`);

UPDATE `customers`
SET `customer_id` = `id`
WHERE `customer_id` IS NULL;

ALTER TABLE `drivers`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_drivers_customer_id` ON `drivers` (`customer_id`);

ALTER TABLE `job_expected_items`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_job_expected_items_customer_id` ON `job_expected_items` (`customer_id`);

ALTER TABLE `linen_assignments`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_linen_assignments_customer_id` ON `linen_assignments` (`customer_id`);

ALTER TABLE `linen_assignment_events`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_linen_assignment_events_customer_id` ON `linen_assignment_events` (`customer_id`);

ALTER TABLE `linen_items`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_linen_items_customer_id` ON `linen_items` (`customer_id`);

UPDATE `linen_items`
SET `customer_id` = `owner_customer_id`
WHERE `customer_id` IS NULL AND `owner_customer_id` IS NOT NULL;

ALTER TABLE `linen_movement_events`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_linen_movement_events_customer_id` ON `linen_movement_events` (`customer_id`);

ALTER TABLE `linen_quality_events`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_linen_quality_events_customer_id` ON `linen_quality_events` (`customer_id`);

ALTER TABLE `locations`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL;
CREATE INDEX IF NOT EXISTS `ix_locations_customer_id` ON `locations` (`customer_id`);

ALTER TABLE `readers`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_readers_customer_id` ON `readers` (`customer_id`);

ALTER TABLE `reader_events`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_reader_events_customer_id` ON `reader_events` (`customer_id`);

ALTER TABLE `reader_ways`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_reader_ways_customer_id` ON `reader_ways` (`customer_id`);

ALTER TABLE `reader_way_events`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_reader_way_events_customer_id` ON `reader_way_events` (`customer_id`);

ALTER TABLE `rfid_scan_raw`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_rfid_scan_raw_customer_id` ON `rfid_scan_raw` (`customer_id`);

ALTER TABLE `system_settings`
    ADD COLUMN IF NOT EXISTS `customer_id` BIGINT UNSIGNED NULL AFTER `id`;
CREATE INDEX IF NOT EXISTS `ix_system_settings_customer_id` ON `system_settings` (`customer_id`);

-- Optional foreign keys (run only once after data is valid and cleaned):
-- ALTER TABLE `drivers` ADD CONSTRAINT `fk_drivers_customer`
--   FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
