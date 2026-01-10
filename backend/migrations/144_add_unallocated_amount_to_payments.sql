-- Add unallocated_amount column to payments table (required by trigger)
ALTER TABLE payments
    ADD COLUMN IF NOT EXISTS unallocated_amount NUMERIC(18, 2);

-- Initialize unallocated_amount to the full payment amount for existing records
UPDATE payments
SET unallocated_amount = amount
WHERE unallocated_amount IS NULL;

-- Also add to vendor_payments for consistency
ALTER TABLE vendor_payments
    ADD COLUMN IF NOT EXISTS unallocated_amount NUMERIC(18, 2);

UPDATE vendor_payments
SET unallocated_amount = amount
WHERE unallocated_amount IS NULL;

-- Create trigger for vendor_payment_allocations too
CREATE OR REPLACE FUNCTION update_vendor_payment_unallocated_amount()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        UPDATE vendor_payments
        SET unallocated_amount = amount - COALESCE((
            SELECT SUM(allocated_amount) FROM vendor_payment_allocations
            WHERE vendor_payment_id = OLD.vendor_payment_id
        ), 0),
            updated_at = CURRENT_TIMESTAMP
        WHERE id = OLD.vendor_payment_id;
        RETURN OLD;
    ELSE
        UPDATE vendor_payments
        SET unallocated_amount = amount - COALESCE((
            SELECT SUM(allocated_amount) FROM vendor_payment_allocations
            WHERE vendor_payment_id = NEW.vendor_payment_id
        ), 0),
            updated_at = CURRENT_TIMESTAMP
        WHERE id = NEW.vendor_payment_id;
        RETURN NEW;
    END IF;
END;
$$;

DROP TRIGGER IF EXISTS trg_update_vendor_payment_unallocated ON vendor_payment_allocations;

CREATE TRIGGER trg_update_vendor_payment_unallocated
AFTER INSERT OR UPDATE OR DELETE ON vendor_payment_allocations
FOR EACH ROW EXECUTE FUNCTION update_vendor_payment_unallocated_amount();
