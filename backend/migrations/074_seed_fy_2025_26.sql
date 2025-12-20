-- Migration: 101_seed_fy_2025_26.sql
-- Description: Seed FY 2025-26 tax rule pack with latest Budget 2025 rates

-- Insert FY 2025-26 Rule Pack
INSERT INTO tax_rule_packs (
    pack_code,
    pack_name,
    financial_year,
    version,
    source_notification,
    description,
    status,
    income_tax_slabs,
    standard_deductions,
    rebate_thresholds,
    cess_rates,
    surcharge_rates,
    tds_rates,
    pf_esi_rates,
    professional_tax_config,
    gst_rates,
    created_by,
    activated_at,
    activated_by
) VALUES (
    'FY_2025_26_V1',
    'FY 2025-26 Tax Rules (Budget 2025)',
    '2025-26',
    1,
    'Finance Act 2025',
    'Tax rules as per Union Budget 2025 effective from April 1, 2025',
    'active',

    -- Income Tax Slabs (New and Old Regime)
    '{
        "new": [
            {"min": 0, "max": 400000, "rate": 0, "description": "Up to 4 Lakhs"},
            {"min": 400001, "max": 800000, "rate": 5, "description": "4L to 8L"},
            {"min": 800001, "max": 1200000, "rate": 10, "description": "8L to 12L"},
            {"min": 1200001, "max": 1600000, "rate": 15, "description": "12L to 16L"},
            {"min": 1600001, "max": 2000000, "rate": 20, "description": "16L to 20L"},
            {"min": 2000001, "max": 2400000, "rate": 25, "description": "20L to 24L"},
            {"min": 2400001, "max": null, "rate": 30, "description": "Above 24L"}
        ],
        "old": [
            {"min": 0, "max": 250000, "rate": 0, "description": "Up to 2.5 Lakhs"},
            {"min": 250001, "max": 500000, "rate": 5, "description": "2.5L to 5L"},
            {"min": 500001, "max": 1000000, "rate": 20, "description": "5L to 10L"},
            {"min": 1000001, "max": null, "rate": 30, "description": "Above 10L"}
        ],
        "senior_citizen_old": [
            {"min": 0, "max": 300000, "rate": 0, "description": "Up to 3 Lakhs"},
            {"min": 300001, "max": 500000, "rate": 5, "description": "3L to 5L"},
            {"min": 500001, "max": 1000000, "rate": 20, "description": "5L to 10L"},
            {"min": 1000001, "max": null, "rate": 30, "description": "Above 10L"}
        ],
        "super_senior_old": [
            {"min": 0, "max": 500000, "rate": 0, "description": "Up to 5 Lakhs"},
            {"min": 500001, "max": 1000000, "rate": 20, "description": "5L to 10L"},
            {"min": 1000001, "max": null, "rate": 30, "description": "Above 10L"}
        ]
    }'::JSONB,

    -- Standard Deductions
    '{
        "new": 75000,
        "old": 50000,
        "family_pension": 15000
    }'::JSONB,

    -- Rebate Thresholds (Section 87A)
    '{
        "new": {
            "income_threshold": 1200000,
            "max_rebate": 60000,
            "effective_tax_free": 1275000,
            "description": "Full tax rebate up to Rs 12L taxable income under new regime"
        },
        "old": {
            "income_threshold": 500000,
            "max_rebate": 12500,
            "description": "Rebate under old regime"
        }
    }'::JSONB,

    -- Cess Rates
    '{
        "health_education_cess": 4,
        "description": "4% Health and Education Cess on tax + surcharge"
    }'::JSONB,

    -- Surcharge Rates
    '{
        "slabs": [
            {"min": 5000000, "max": 10000000, "rate": 10},
            {"min": 10000001, "max": 20000000, "rate": 15},
            {"min": 20000001, "max": 50000000, "rate": 25},
            {"min": 50000001, "max": null, "rate": 37}
        ],
        "max_surcharge_new_regime": 25,
        "marginal_relief_applicable": true
    }'::JSONB,

    -- TDS Rates (Updated for FY 2025-26)
    '{
        "194A": {
            "rate": 10,
            "threshold_senior": 50000,
            "threshold_other": 40000,
            "description": "Interest other than securities"
        },
        "194B": {
            "rate": 30,
            "threshold": 10000,
            "description": "Lottery winnings"
        },
        "194C": {
            "rate_individual": 1,
            "rate_other": 2,
            "threshold_single": 30000,
            "threshold_aggregate": 100000,
            "description": "Contractor payments"
        },
        "194H": {
            "rate": 2,
            "threshold": 15000,
            "description": "Commission/Brokerage (reduced from 5%)"
        },
        "194I": {
            "rate_land_building": 10,
            "rate_plant_machinery": 2,
            "threshold": 240000,
            "description": "Rent"
        },
        "194J": {
            "rate_professional": 10,
            "rate_technical_royalty": 2,
            "threshold": 50000,
            "description": "Professional/Technical fees (threshold increased from 30K)"
        },
        "194T": {
            "rate": 10,
            "threshold": 20000,
            "description": "Payment to partners (NEW section from FY 2025-26)"
        },
        "no_pan_rate": 20,
        "lower_deduction_certificate": true
    }'::JSONB,

    -- PF/ESI Rates
    '{
        "pf": {
            "employee_contribution": 12,
            "employer_contribution": 12,
            "employer_epf": 3.67,
            "employer_eps": 8.33,
            "admin_charges": 0.5,
            "edli": 0.5,
            "wage_ceiling": 15000,
            "voluntary_above_ceiling": true
        },
        "esi": {
            "employee_contribution": 0.75,
            "employer_contribution": 3.25,
            "wage_ceiling": 21000,
            "effective_date": "2024-01-01"
        },
        "gratuity": {
            "calculation_factor": 15,
            "divisor": 26,
            "tax_free_limit": 2000000
        }
    }'::JSONB,

    -- Professional Tax (State-wise)
    '{
        "KA": {
            "name": "Karnataka",
            "slabs": [
                {"min": 0, "max": 25000, "amount": 0},
                {"min": 25001, "max": null, "amount": 200}
            ],
            "february_additional": 0,
            "annual_max": 2400
        },
        "MH": {
            "name": "Maharashtra",
            "slabs": [
                {"min": 0, "max": 7500, "amount": 0},
                {"min": 7501, "max": 10000, "amount": 175},
                {"min": 10001, "max": null, "amount": 200}
            ],
            "february_additional": 100,
            "annual_max": 2500
        },
        "TN": {
            "name": "Tamil Nadu",
            "slabs": [
                {"min": 0, "max": 21000, "amount": 0},
                {"min": 21001, "max": 30000, "amount": 100},
                {"min": 30001, "max": 45000, "amount": 235},
                {"min": 45001, "max": 60000, "amount": 510},
                {"min": 60001, "max": 75000, "amount": 760},
                {"min": 75001, "max": null, "amount": 1095}
            ],
            "frequency": "half_yearly"
        },
        "WB": {
            "name": "West Bengal",
            "slabs": [
                {"min": 0, "max": 10000, "amount": 0},
                {"min": 10001, "max": 15000, "amount": 110},
                {"min": 15001, "max": 25000, "amount": 130},
                {"min": 25001, "max": 40000, "amount": 150},
                {"min": 40001, "max": null, "amount": 200}
            ]
        },
        "GJ": {
            "name": "Gujarat",
            "slabs": [
                {"min": 0, "max": 5999, "amount": 0},
                {"min": 6000, "max": 8999, "amount": 80},
                {"min": 9000, "max": 11999, "amount": 150},
                {"min": 12000, "max": null, "amount": 200}
            ]
        },
        "AP": {
            "name": "Andhra Pradesh",
            "slabs": [
                {"min": 0, "max": 15000, "amount": 0},
                {"min": 15001, "max": 20000, "amount": 150},
                {"min": 20001, "max": null, "amount": 200}
            ]
        },
        "TS": {
            "name": "Telangana",
            "slabs": [
                {"min": 0, "max": 15000, "amount": 0},
                {"min": 15001, "max": 20000, "amount": 150},
                {"min": 20001, "max": null, "amount": 200}
            ]
        },
        "KL": {
            "name": "Kerala",
            "slabs": [
                {"min": 0, "max": 11999, "amount": 0},
                {"min": 12000, "max": 17999, "amount": 120},
                {"min": 18000, "max": 29999, "amount": 180},
                {"min": 30000, "max": null, "amount": 250}
            ],
            "annual_max": 2500
        }
    }'::JSONB,

    -- GST Rates
    '{
        "standard_rates": [0, 5, 12, 18, 28],
        "composition_limit": 15000000,
        "gstr1_due_date": 11,
        "gstr3b_due_date": 20,
        "e_invoice_threshold": 50000000,
        "e_invoice_30_day_limit_threshold": 100000000,
        "einvoice_30_day_reporting": true
    }'::JSONB,

    'system',
    NOW(),
    'system'
);

-- Insert TDS Section Rates for FY 2025-26
INSERT INTO tds_section_rates (
    rule_pack_id,
    section_code,
    section_name,
    rate_individual,
    rate_company,
    rate_no_pan,
    threshold_amount,
    threshold_type,
    payee_types,
    effective_from,
    notes
)
SELECT
    id as rule_pack_id,
    section_code,
    section_name,
    rate_individual,
    rate_company,
    rate_no_pan,
    threshold_amount,
    threshold_type,
    payee_types,
    effective_from,
    notes
FROM tax_rule_packs
CROSS JOIN (VALUES
    ('194A', 'Interest (other than securities)', 10, 10, 20, 40000, 'annual', ARRAY['individual', 'company', 'partnership'], '2025-04-01'::DATE, 'Bank interest, FD interest, etc.'),
    ('194B', 'Lottery/Crossword Puzzle Winnings', 30, 30, 30, 10000, 'per_transaction', ARRAY['individual'], '2025-04-01'::DATE, 'Flat 30% on winnings'),
    ('194C', 'Contractor Payment', 1, 2, 20, 30000, 'per_transaction', ARRAY['individual', 'huf', 'company', 'partnership'], '2025-04-01'::DATE, 'Aggregate threshold Rs 1L per FY'),
    ('194H', 'Commission/Brokerage', 2, 2, 20, 15000, 'annual', ARRAY['individual', 'company'], '2025-04-01'::DATE, 'Rate reduced from 5% to 2% in Budget 2025'),
    ('194I_LB', 'Rent - Land/Building', 10, 10, 20, 240000, 'annual', ARRAY['individual', 'company'], '2025-04-01'::DATE, 'Rent for land and building'),
    ('194I_PM', 'Rent - Plant/Machinery', 2, 2, 20, 240000, 'annual', ARRAY['individual', 'company'], '2025-04-01'::DATE, 'Rent for plant and machinery'),
    ('194J_P', 'Professional Services', 10, 10, 20, 50000, 'annual', ARRAY['individual', 'company'], '2025-04-01'::DATE, 'Threshold increased from Rs 30K to Rs 50K'),
    ('194J_T', 'Technical/Royalty', 2, 2, 20, 50000, 'annual', ARRAY['individual', 'company'], '2025-04-01'::DATE, 'Technical services and royalty'),
    ('194T', 'Payment to Partners', 10, NULL, 20, 20000, 'annual', ARRAY['individual'], '2025-04-01'::DATE, 'NEW section - 10% TDS on payments to partners exceeding Rs 20K')
) AS rates(section_code, section_name, rate_individual, rate_company, rate_no_pan, threshold_amount, threshold_type, payee_types, effective_from, notes)
WHERE tax_rule_packs.financial_year = '2025-26' AND tax_rule_packs.version = 1;

-- Also insert FY 2024-25 for reference (superseded status)
INSERT INTO tax_rule_packs (
    pack_code,
    pack_name,
    financial_year,
    version,
    source_notification,
    description,
    status,
    income_tax_slabs,
    standard_deductions,
    rebate_thresholds,
    cess_rates,
    tds_rates,
    pf_esi_rates,
    created_by
) VALUES (
    'FY_2024_25_V1',
    'FY 2024-25 Tax Rules',
    '2024-25',
    1,
    'Finance Act 2024',
    'Tax rules for FY 2024-25 (April 2024 - March 2025)',
    'superseded',

    -- Income Tax Slabs (FY 2024-25)
    '{
        "new": [
            {"min": 0, "max": 300000, "rate": 0},
            {"min": 300001, "max": 700000, "rate": 5},
            {"min": 700001, "max": 1000000, "rate": 10},
            {"min": 1000001, "max": 1200000, "rate": 15},
            {"min": 1200001, "max": 1500000, "rate": 20},
            {"min": 1500001, "max": null, "rate": 30}
        ],
        "old": [
            {"min": 0, "max": 250000, "rate": 0},
            {"min": 250001, "max": 500000, "rate": 5},
            {"min": 500001, "max": 1000000, "rate": 20},
            {"min": 1000001, "max": null, "rate": 30}
        ]
    }'::JSONB,

    '{"new": 75000, "old": 50000}'::JSONB,
    '{"new": {"income_threshold": 700000, "max_rebate": 25000}}'::JSONB,
    '{"health_education_cess": 4}'::JSONB,

    '{
        "194C": {"rate_individual": 1, "rate_other": 2},
        "194H": {"rate": 5, "threshold": 15000},
        "194J": {"rate": 10, "threshold": 30000}
    }'::JSONB,

    '{
        "pf": {"employee_contribution": 12, "employer_contribution": 12},
        "esi": {"employee_contribution": 0.75, "employer_contribution": 3.25}
    }'::JSONB,

    'system'
);

-- Create index for faster FY lookups
CREATE INDEX IF NOT EXISTS idx_tax_rule_packs_fy_active
ON tax_rule_packs(financial_year)
WHERE status = 'active';

COMMENT ON TABLE tax_rule_packs IS 'FY 2025-26 rule pack includes Budget 2025 changes: new slabs, 194J threshold increase, 194H rate reduction, new 194T section';
