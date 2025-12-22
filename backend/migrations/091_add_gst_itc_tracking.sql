-- Migration: 091_add_gst_itc_tracking.sql
-- Description: Add GST Input Tax Credit (ITC) tracking for expenses
-- Per ICAI guidelines and GST Act 2017, ITC can be claimed on business purchases

-- =====================================================
-- PART 1: Add GST columns to expense_claims table
-- =====================================================

-- Add vendor and GST information to expense claims
ALTER TABLE expense_claims
ADD COLUMN IF NOT EXISTS vendor_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS vendor_gstin VARCHAR(15),
ADD COLUMN IF NOT EXISTS invoice_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS invoice_date DATE,
ADD COLUMN IF NOT EXISTS is_gst_applicable BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS supply_type VARCHAR(20) DEFAULT 'intra_state', -- intra_state, inter_state
ADD COLUMN IF NOT EXISTS hsn_sac_code VARCHAR(10),
ADD COLUMN IF NOT EXISTS gst_rate DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS base_amount DECIMAL(18,2), -- Amount before GST
ADD COLUMN IF NOT EXISTS cgst_rate DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS cgst_amount DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS sgst_rate DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS sgst_amount DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS igst_rate DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS igst_amount DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS cess_rate DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS cess_amount DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_gst_amount DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS itc_eligible BOOLEAN DEFAULT TRUE, -- Some expenses not eligible for ITC
ADD COLUMN IF NOT EXISTS itc_claimed BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS itc_claimed_in_return VARCHAR(20); -- GSTR-3B month, e.g., "Jan-2025"

-- Add GST settings to expense categories
ALTER TABLE expense_categories
ADD COLUMN IF NOT EXISTS is_gst_applicable BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS default_gst_rate DECIMAL(5,2) DEFAULT 18,
ADD COLUMN IF NOT EXISTS default_hsn_sac VARCHAR(10),
ADD COLUMN IF NOT EXISTS itc_eligible BOOLEAN DEFAULT TRUE; -- Some categories blocked ITC (motor vehicles, food, etc.)

-- =====================================================
-- PART 2: Create GST Input Credit tracking table
-- Similar to TDS receivable - tracks ITC for GSTR returns
-- =====================================================

CREATE TABLE IF NOT EXISTS gst_input_credit (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    financial_year VARCHAR(10) NOT NULL, -- "2024-25"
    return_period VARCHAR(20) NOT NULL, -- "Jan-2025" (GSTR-3B filing month)

    -- Source document
    source_type VARCHAR(50) NOT NULL, -- expense_claim, subscription, contractor_payment, asset_purchase
    source_id UUID NOT NULL,
    source_number VARCHAR(100), -- claim number / invoice number

    -- Vendor details (for GSTR-2B matching)
    vendor_gstin VARCHAR(15),
    vendor_name VARCHAR(255),
    vendor_invoice_number VARCHAR(100),
    vendor_invoice_date DATE,

    -- GST details
    place_of_supply VARCHAR(50), -- State code
    supply_type VARCHAR(20) DEFAULT 'intra_state',
    hsn_sac_code VARCHAR(10),
    taxable_value DECIMAL(18,2) NOT NULL,
    cgst_rate DECIMAL(5,2) DEFAULT 0,
    cgst_amount DECIMAL(18,2) DEFAULT 0,
    sgst_rate DECIMAL(5,2) DEFAULT 0,
    sgst_amount DECIMAL(18,2) DEFAULT 0,
    igst_rate DECIMAL(5,2) DEFAULT 0,
    igst_amount DECIMAL(18,2) DEFAULT 0,
    cess_rate DECIMAL(5,2) DEFAULT 0,
    cess_amount DECIMAL(18,2) DEFAULT 0,
    total_gst DECIMAL(18,2) NOT NULL,

    -- ITC eligibility
    itc_eligible BOOLEAN DEFAULT TRUE,
    ineligible_reason VARCHAR(255), -- If not eligible, why (blocked credit, personal use, etc.)

    -- GSTR-2B matching status
    matched_with_gstr2b BOOLEAN DEFAULT FALSE,
    gstr2b_match_date DATE,
    gstr2b_mismatch_reason VARCHAR(255),

    -- Claim status
    status VARCHAR(50) DEFAULT 'pending', -- pending, claimed, reversed, rejected
    claimed_in_gstr3b BOOLEAN DEFAULT FALSE,
    gstr3b_filing_period VARCHAR(20), -- When actually claimed in GSTR-3B
    claimed_at TIMESTAMP,
    claimed_by VARCHAR(255),

    -- Reversal (Rule 42/43 or blocked credit)
    is_reversed BOOLEAN DEFAULT FALSE,
    reversal_amount DECIMAL(18,2),
    reversal_reason VARCHAR(255),
    reversal_date DATE,

    -- Audit
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Indexes for efficient queries
CREATE INDEX IF NOT EXISTS idx_gst_input_company ON gst_input_credit(company_id);
CREATE INDEX IF NOT EXISTS idx_gst_input_period ON gst_input_credit(return_period);
CREATE INDEX IF NOT EXISTS idx_gst_input_vendor ON gst_input_credit(vendor_gstin);
CREATE INDEX IF NOT EXISTS idx_gst_input_status ON gst_input_credit(status);
CREATE INDEX IF NOT EXISTS idx_gst_input_source ON gst_input_credit(source_type, source_id);
CREATE INDEX IF NOT EXISTS idx_gst_input_fy ON gst_input_credit(financial_year);

-- =====================================================
-- PART 3: Create posting rules for expenses with GST
-- Per ICAI AS 16 - Property, Plant & Equipment and AS 2 - Inventories
-- =====================================================

INSERT INTO posting_rules (
    company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json, posting_template,
    financial_year, effective_from, priority, is_active, created_by, created_at, updated_at
)
SELECT
    NULL as company_id, -- Global rule
    'EXP_GST_INTRA' as rule_code,
    'Expense with GST (Intra-State CGST+SGST)' as rule_name,
    'expense_claim' as source_type,
    'on_reimburse' as trigger_event,
    '{"is_gst_applicable": true, "supply_type": "intra_state"}' as conditions_json,
    '{
        "lines": [
            {
                "account_code_field": "expense_account",
                "account_code_fallback": "5100",
                "debit_field": "base_amount",
                "credit_field": null,
                "description_template": "Expense: {title} - {vendor_name}"
            },
            {
                "account_code": "1141",
                "debit_field": "cgst_amount",
                "credit_field": null,
                "description_template": "CGST Input @ {cgst_rate}% on {vendor_invoice_number}"
            },
            {
                "account_code": "1142",
                "debit_field": "sgst_amount",
                "credit_field": null,
                "description_template": "SGST Input @ {sgst_rate}% on {vendor_invoice_number}"
            },
            {
                "account_code": "2102",
                "debit_field": null,
                "credit_field": "amount",
                "subledger_type": "employee",
                "subledger_id_field": "employee_id",
                "description_template": "Payable to {employee_name} for {title}"
            }
        ],
        "narration_template": "Expense claim {claim_number}: {title} from {vendor_name}. Invoice: {vendor_invoice_number}"
    }' as posting_template,
    NULL as financial_year,
    '2024-04-01'::date as effective_from,
    10 as priority,
    TRUE as is_active,
    NULL as created_by,
    NOW() as created_at,
    NOW() as updated_at
WHERE NOT EXISTS (SELECT 1 FROM posting_rules WHERE rule_code = 'EXP_GST_INTRA');

INSERT INTO posting_rules (
    company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json, posting_template,
    financial_year, effective_from, priority, is_active, created_by, created_at, updated_at
)
SELECT
    NULL as company_id,
    'EXP_GST_INTER' as rule_code,
    'Expense with GST (Inter-State IGST)' as rule_name,
    'expense_claim' as source_type,
    'on_reimburse' as trigger_event,
    '{"is_gst_applicable": true, "supply_type": "inter_state"}' as conditions_json,
    '{
        "lines": [
            {
                "account_code_field": "expense_account",
                "account_code_fallback": "5100",
                "debit_field": "base_amount",
                "credit_field": null,
                "description_template": "Expense: {title} - {vendor_name}"
            },
            {
                "account_code": "1143",
                "debit_field": "igst_amount",
                "credit_field": null,
                "description_template": "IGST Input @ {igst_rate}% on {vendor_invoice_number}"
            },
            {
                "account_code": "2102",
                "debit_field": null,
                "credit_field": "amount",
                "subledger_type": "employee",
                "subledger_id_field": "employee_id",
                "description_template": "Payable to {employee_name} for {title}"
            }
        ],
        "narration_template": "Expense claim {claim_number}: {title} from {vendor_name} (Inter-state). Invoice: {vendor_invoice_number}"
    }' as posting_template,
    NULL as financial_year,
    '2024-04-01'::date as effective_from,
    10 as priority,
    TRUE as is_active,
    NULL as created_by,
    NOW() as created_at,
    NOW() as updated_at
WHERE NOT EXISTS (SELECT 1 FROM posting_rules WHERE rule_code = 'EXP_GST_INTER');

-- Rule for expense without GST (simple reimbursement)
INSERT INTO posting_rules (
    company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json, posting_template,
    financial_year, effective_from, priority, is_active, created_by, created_at, updated_at
)
SELECT
    NULL as company_id,
    'EXP_NO_GST' as rule_code,
    'Expense without GST' as rule_name,
    'expense_claim' as source_type,
    'on_reimburse' as trigger_event,
    '{"is_gst_applicable": false}' as conditions_json,
    '{
        "lines": [
            {
                "account_code_field": "expense_account",
                "account_code_fallback": "5100",
                "debit_field": "amount",
                "credit_field": null,
                "description_template": "Expense: {title}"
            },
            {
                "account_code": "2102",
                "debit_field": null,
                "credit_field": "amount",
                "subledger_type": "employee",
                "subledger_id_field": "employee_id",
                "description_template": "Payable to {employee_name} for {title}"
            }
        ],
        "narration_template": "Expense claim {claim_number}: {title}"
    }' as posting_template,
    NULL as financial_year,
    '2024-04-01'::date as effective_from,
    20 as priority,
    TRUE as is_active,
    NULL as created_by,
    NOW() as created_at,
    NOW() as updated_at
WHERE NOT EXISTS (SELECT 1 FROM posting_rules WHERE rule_code = 'EXP_NO_GST');

-- =====================================================
-- PART 4: Update expense categories with GST defaults
-- Based on common Indian business expense categories
-- =====================================================

-- Update Travel category - GST on hotels, flights, etc.
UPDATE expense_categories
SET is_gst_applicable = TRUE,
    default_gst_rate = 18,
    default_hsn_sac = '9963', -- Accommodation services
    itc_eligible = TRUE
WHERE LOWER(name) LIKE '%travel%' OR LOWER(name) LIKE '%hotel%';

-- Update Software/Subscription category - GST on SaaS
UPDATE expense_categories
SET is_gst_applicable = TRUE,
    default_gst_rate = 18,
    default_hsn_sac = '998314', -- IT services
    itc_eligible = TRUE
WHERE LOWER(name) LIKE '%software%' OR LOWER(name) LIKE '%subscription%' OR LOWER(name) LIKE '%saas%';

-- Update Office Supplies category
UPDATE expense_categories
SET is_gst_applicable = TRUE,
    default_gst_rate = 18,
    default_hsn_sac = '4820', -- Stationery
    itc_eligible = TRUE
WHERE LOWER(name) LIKE '%office%' OR LOWER(name) LIKE '%supplies%' OR LOWER(name) LIKE '%stationery%';

-- Food & Entertainment - Blocked ITC per GST Act Section 17(5)
UPDATE expense_categories
SET is_gst_applicable = TRUE,
    default_gst_rate = 5, -- Restaurant GST
    default_hsn_sac = '9963', -- Restaurant services
    itc_eligible = FALSE -- Blocked credit on food & beverages
WHERE LOWER(name) LIKE '%food%' OR LOWER(name) LIKE '%meal%' OR LOWER(name) LIKE '%entertainment%';

-- Fuel - Blocked ITC per GST Act Section 17(5)
UPDATE expense_categories
SET is_gst_applicable = TRUE,
    default_gst_rate = 18,
    default_hsn_sac = '2710', -- Petroleum
    itc_eligible = FALSE -- Blocked credit on motor vehicle fuel
WHERE LOWER(name) LIKE '%fuel%' OR LOWER(name) LIKE '%petrol%' OR LOWER(name) LIKE '%diesel%';

-- Medical/Healthcare - Usually exempt from GST
UPDATE expense_categories
SET is_gst_applicable = FALSE,
    default_gst_rate = 0,
    itc_eligible = FALSE
WHERE LOWER(name) LIKE '%medical%' OR LOWER(name) LIKE '%health%' OR LOWER(name) LIKE '%insurance%';

-- =====================================================
-- PART 5: Comments for CA Review
-- =====================================================
/*
CA REVIEW POINTS - GST Input Tax Credit Implementation:

1. ITC ELIGIBILITY (Section 16-21, GST Act):
   - ITC available only on goods/services used for business
   - Vendor must be registered and file returns
   - Invoice must have correct GSTIN of recipient
   - Goods/services must be received

2. BLOCKED CREDITS (Section 17(5)):
   - Motor vehicles (except for certain businesses)
   - Food and beverages
   - Beauty treatment, health services
   - Works contract for immovable property
   - Personal consumption

3. MATCHING WITH GSTR-2B:
   - Auto-generated from supplier's GSTR-1
   - Must match before claiming in GSTR-3B
   - Unmatched ITC may require reversal

4. REVERSAL RULES (Rule 42/43):
   - Common credit reversal for exempt supplies
   - Input Service Distributor rules
   - Time limit for claiming ITC (180 days)

5. JOURNAL ENTRIES IMPLEMENTED:
   - EXP_GST_INTRA: Dr. Expense + Dr. CGST Input + Dr. SGST Input, Cr. Payable
   - EXP_GST_INTER: Dr. Expense + Dr. IGST Input, Cr. Payable
   - EXP_NO_GST: Dr. Expense, Cr. Payable

6. ACCOUNTS USED:
   - 1141: CGST Input (Current Asset)
   - 1142: SGST Input (Current Asset)
   - 1143: IGST Input (Current Asset)
   - 1144: GST Cess Input (Current Asset)
   - 2102: Employee Payables (Current Liability)
   - 5100: General Expense (Expense)

Please validate:
- [ ] ITC eligibility logic is correct
- [ ] Blocked credit categories are comprehensive
- [ ] Journal entry pattern follows ICAI guidelines
- [ ] GSTR-2B matching workflow is adequate
- [ ] Reversal scenarios are covered
*/
