-- Optional: run after deploying workflow + employee assignment features.
-- Links linen_items.assigned_employee_id to employees(id).

ALTER TABLE linen_items
  ADD COLUMN assigned_employee_id BIGINT UNSIGNED NULL AFTER owner_customer_id;

ALTER TABLE linen_items
  ADD CONSTRAINT fk_linen_items_assigned_employee
  FOREIGN KEY (assigned_employee_id) REFERENCES employees(id)
  ON DELETE SET NULL;
