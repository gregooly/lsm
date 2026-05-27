-- Locations upgrade for operations profile + filtering.
-- Run once on MySQL 8+.

ALTER TABLE locations
    ADD COLUMN customer_id BIGINT UNSIGNED NULL AFTER location_type,
    ADD COLUMN contact_person VARCHAR(120) NULL AFTER location_address_text,
    ADD COLUMN contact_phone VARCHAR(30) NULL AFTER contact_person,
    ADD COLUMN geo_lat DECIMAL(10,7) NULL AFTER contact_phone,
    ADD COLUMN geo_lng DECIMAL(10,7) NULL AFTER geo_lat;

ALTER TABLE locations
    ADD CONSTRAINT fk_locations_customer
    FOREIGN KEY (customer_id) REFERENCES customers (id)
    ON DELETE SET NULL ON UPDATE CASCADE;

CREATE INDEX IF NOT EXISTS idx_locations_type_active
    ON locations (location_type, is_active);

CREATE INDEX IF NOT EXISTS idx_locations_customer
    ON locations (customer_id);

CREATE INDEX IF NOT EXISTS idx_locations_name
    ON locations (location_name);
