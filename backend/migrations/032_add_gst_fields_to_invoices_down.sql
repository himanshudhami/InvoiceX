-- Rollback: Remove GST fields from invoices table

DROP INDEX IF EXISTS idx_invoices_invoice_type;
DROP INDEX IF EXISTS idx_invoices_supply_type;
DROP INDEX IF EXISTS idx_invoices_e_invoice_irn;

ALTER TABLE invoices DROP COLUMN IF EXISTS invoice_type;
ALTER TABLE invoices DROP COLUMN IF EXISTS supply_type;
ALTER TABLE invoices DROP COLUMN IF EXISTS place_of_supply;
ALTER TABLE invoices DROP COLUMN IF EXISTS reverse_charge;
ALTER TABLE invoices DROP COLUMN IF EXISTS total_cgst;
ALTER TABLE invoices DROP COLUMN IF EXISTS total_sgst;
ALTER TABLE invoices DROP COLUMN IF EXISTS total_igst;
ALTER TABLE invoices DROP COLUMN IF EXISTS total_cess;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_applicable;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_irn;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_ack_number;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_ack_date;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_qr_code;
ALTER TABLE invoices DROP COLUMN IF EXISTS shipping_address;
ALTER TABLE invoices DROP COLUMN IF EXISTS transporter_name;
ALTER TABLE invoices DROP COLUMN IF EXISTS vehicle_number;
ALTER TABLE invoices DROP COLUMN IF EXISTS eway_bill_number;
