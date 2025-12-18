-- 056_add_employee_hierarchy.sql
-- Add hierarchy fields to employees table for manager relationships and org structure

-- Add hierarchy columns to employees table
ALTER TABLE employees
ADD COLUMN IF NOT EXISTS manager_id UUID REFERENCES employees(id) ON DELETE SET NULL,
ADD COLUMN IF NOT EXISTS reporting_level INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS is_manager BOOLEAN NOT NULL DEFAULT FALSE;

-- Create index for efficient hierarchy queries
CREATE INDEX IF NOT EXISTS idx_employees_manager_id ON employees(manager_id);
CREATE INDEX IF NOT EXISTS idx_employees_company_manager ON employees(company_id, manager_id);
CREATE INDEX IF NOT EXISTS idx_employees_is_manager ON employees(is_manager) WHERE is_manager = TRUE;

-- Function to prevent circular references in hierarchy
CREATE OR REPLACE FUNCTION check_employee_hierarchy_circular_reference()
RETURNS TRIGGER AS $$
DECLARE
    current_manager_id UUID;
    visited_ids UUID[];
BEGIN
    -- If no manager assigned, no circular reference possible
    IF NEW.manager_id IS NULL THEN
        RETURN NEW;
    END IF;

    -- Cannot be your own manager
    IF NEW.manager_id = NEW.id THEN
        RAISE EXCEPTION 'An employee cannot be their own manager';
    END IF;

    -- Walk up the hierarchy to check for cycles
    current_manager_id := NEW.manager_id;
    visited_ids := ARRAY[NEW.id];

    WHILE current_manager_id IS NOT NULL LOOP
        -- Check if we've created a cycle
        IF current_manager_id = ANY(visited_ids) THEN
            RAISE EXCEPTION 'Circular reference detected in employee hierarchy';
        END IF;

        visited_ids := array_append(visited_ids, current_manager_id);

        -- Get the next manager up the chain
        SELECT manager_id INTO current_manager_id
        FROM employees
        WHERE id = current_manager_id;
    END LOOP;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to enforce no circular references
DROP TRIGGER IF EXISTS trg_check_employee_hierarchy ON employees;
CREATE TRIGGER trg_check_employee_hierarchy
    BEFORE INSERT OR UPDATE OF manager_id ON employees
    FOR EACH ROW
    EXECUTE FUNCTION check_employee_hierarchy_circular_reference();

-- Function to auto-update is_manager flag when employees are assigned to a manager
CREATE OR REPLACE FUNCTION update_is_manager_flag()
RETURNS TRIGGER AS $$
BEGIN
    -- When manager_id is set, mark that manager as a manager
    IF NEW.manager_id IS NOT NULL THEN
        UPDATE employees
        SET is_manager = TRUE, updated_at = NOW()
        WHERE id = NEW.manager_id AND is_manager = FALSE;
    END IF;

    -- When manager_id is removed (OLD.manager_id is not null but NEW.manager_id is null)
    -- Check if old manager still has any reports
    IF TG_OP = 'UPDATE' AND OLD.manager_id IS NOT NULL AND (NEW.manager_id IS NULL OR NEW.manager_id != OLD.manager_id) THEN
        IF NOT EXISTS (
            SELECT 1 FROM employees
            WHERE manager_id = OLD.manager_id AND id != NEW.id
        ) THEN
            UPDATE employees
            SET is_manager = FALSE, updated_at = NOW()
            WHERE id = OLD.manager_id;
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to auto-manage is_manager flag
DROP TRIGGER IF EXISTS trg_update_is_manager ON employees;
CREATE TRIGGER trg_update_is_manager
    AFTER INSERT OR UPDATE OF manager_id ON employees
    FOR EACH ROW
    EXECUTE FUNCTION update_is_manager_flag();

-- Function to handle employee deletion - reset is_manager for orphaned managers
CREATE OR REPLACE FUNCTION handle_employee_deletion_hierarchy()
RETURNS TRIGGER AS $$
BEGIN
    -- Check if the deleted employee's former manager still has any reports
    IF OLD.manager_id IS NOT NULL THEN
        IF NOT EXISTS (
            SELECT 1 FROM employees
            WHERE manager_id = OLD.manager_id AND id != OLD.id
        ) THEN
            UPDATE employees
            SET is_manager = FALSE, updated_at = NOW()
            WHERE id = OLD.manager_id;
        END IF;
    END IF;

    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

-- Trigger for handling deletions
DROP TRIGGER IF EXISTS trg_employee_deletion_hierarchy ON employees;
CREATE TRIGGER trg_employee_deletion_hierarchy
    AFTER DELETE ON employees
    FOR EACH ROW
    EXECUTE FUNCTION handle_employee_deletion_hierarchy();

-- Function to calculate reporting level (depth in hierarchy)
CREATE OR REPLACE FUNCTION calculate_reporting_level(employee_id_param UUID)
RETURNS INTEGER AS $$
DECLARE
    level INTEGER := 0;
    current_manager_id UUID;
BEGIN
    SELECT manager_id INTO current_manager_id
    FROM employees
    WHERE id = employee_id_param;

    WHILE current_manager_id IS NOT NULL LOOP
        level := level + 1;
        SELECT manager_id INTO current_manager_id
        FROM employees
        WHERE id = current_manager_id;
    END LOOP;

    RETURN level;
END;
$$ LANGUAGE plpgsql;

-- Function to update reporting level on hierarchy change
CREATE OR REPLACE FUNCTION update_reporting_level()
RETURNS TRIGGER AS $$
DECLARE
    new_level INTEGER;
BEGIN
    -- Calculate new reporting level
    new_level := calculate_reporting_level(NEW.id);

    -- Only update if level changed
    IF new_level != NEW.reporting_level THEN
        NEW.reporting_level := new_level;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to auto-update reporting level
DROP TRIGGER IF EXISTS trg_update_reporting_level ON employees;
CREATE TRIGGER trg_update_reporting_level
    BEFORE INSERT OR UPDATE OF manager_id ON employees
    FOR EACH ROW
    EXECUTE FUNCTION update_reporting_level();

-- Recursive function to update all subordinates' reporting levels (called after hierarchy change)
CREATE OR REPLACE FUNCTION cascade_update_reporting_levels(manager_id_param UUID)
RETURNS VOID AS $$
DECLARE
    emp RECORD;
BEGIN
    FOR emp IN
        SELECT id FROM employees WHERE manager_id = manager_id_param
    LOOP
        UPDATE employees
        SET reporting_level = calculate_reporting_level(emp.id),
            updated_at = NOW()
        WHERE id = emp.id;

        -- Recursively update subordinates
        PERFORM cascade_update_reporting_levels(emp.id);
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- Comment on columns for documentation
COMMENT ON COLUMN employees.manager_id IS 'Reference to the employee''s direct manager';
COMMENT ON COLUMN employees.reporting_level IS 'Depth in the org hierarchy (0 = top level, no manager)';
COMMENT ON COLUMN employees.is_manager IS 'TRUE if this employee has at least one direct report';
