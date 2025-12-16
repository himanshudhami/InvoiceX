-- 020_add_fy_2025_26_tax_data.sql
-- Add tax slabs and parameters for Financial Year 2025-26
-- Note: Using same rates as FY 2024-25 until Budget 2025 is announced

-- ============================================================================
-- TAX SLABS FY 2025-26
-- ============================================================================

-- New Tax Regime (Default) - FY 2025-26
INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate) VALUES
('new', '2025-26', 0, 300000, 0, 4),
('new', '2025-26', 300001, 700000, 5, 4),
('new', '2025-26', 700001, 1000000, 10, 4),
('new', '2025-26', 1000001, 1200000, 15, 4),
('new', '2025-26', 1200001, 1500000, 20, 4),
('new', '2025-26', 1500001, NULL, 30, 4)
ON CONFLICT DO NOTHING;

-- Old Tax Regime - FY 2025-26
INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate) VALUES
('old', '2025-26', 0, 250000, 0, 4),
('old', '2025-26', 250001, 500000, 5, 4),
('old', '2025-26', 500001, 1000000, 20, 4),
('old', '2025-26', 1000001, NULL, 30, 4)
ON CONFLICT DO NOTHING;

-- ============================================================================
-- TAX PARAMETERS FY 2025-26
-- ============================================================================

-- New Regime Parameters
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2025-26', 'new', 'STANDARD_DEDUCTION', 'Standard Deduction', 75000, 'amount', 'Standard deduction for salaried employees under new regime', 'Section 16(ia)', '2025-04-01'),
('2025-26', 'new', 'REBATE_87A_THRESHOLD', 'Section 87A Rebate Income Threshold', 700000, 'threshold', 'Taxable income threshold for 87A rebate eligibility under new regime', 'Section 87A', '2025-04-01'),
('2025-26', 'new', 'REBATE_87A_MAX', 'Section 87A Maximum Rebate', 25000, 'amount', 'Maximum rebate amount under Section 87A for new regime', 'Section 87A', '2025-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Old Regime Parameters
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2025-26', 'old', 'STANDARD_DEDUCTION', 'Standard Deduction', 50000, 'amount', 'Standard deduction for salaried employees under old regime', 'Section 16(ia)', '2025-04-01'),
('2025-26', 'old', 'REBATE_87A_THRESHOLD', 'Section 87A Rebate Income Threshold', 500000, 'threshold', 'Taxable income threshold for 87A rebate eligibility under old regime', 'Section 87A', '2025-04-01'),
('2025-26', 'old', 'REBATE_87A_MAX', 'Section 87A Maximum Rebate', 12500, 'amount', 'Maximum rebate amount under Section 87A for old regime', 'Section 87A', '2025-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Common Parameters (both regimes)
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2025-26', 'both', 'CESS_RATE', 'Health & Education Cess Rate', 4, 'percentage', 'Cess on income tax (4% on tax + surcharge)', 'Finance Act 2018', '2025-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Surcharge Thresholds and Rates (both regimes but different caps)
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
-- Surcharge thresholds
('2025-26', 'both', 'SURCHARGE_THRESHOLD_50L', 'Surcharge Threshold 50 Lakh', 5000000, 'threshold', 'Income threshold for 10% surcharge', 'Finance Act', '2025-04-01'),
('2025-26', 'both', 'SURCHARGE_THRESHOLD_1CR', 'Surcharge Threshold 1 Crore', 10000000, 'threshold', 'Income threshold for 15% surcharge', 'Finance Act', '2025-04-01'),
('2025-26', 'both', 'SURCHARGE_THRESHOLD_2CR', 'Surcharge Threshold 2 Crore', 20000000, 'threshold', 'Income threshold for 25% surcharge', 'Finance Act', '2025-04-01'),
('2025-26', 'both', 'SURCHARGE_THRESHOLD_5CR', 'Surcharge Threshold 5 Crore', 50000000, 'threshold', 'Income threshold for 37% surcharge (old regime)', 'Finance Act', '2025-04-01'),

-- Surcharge rates
('2025-26', 'both', 'SURCHARGE_RATE_50L', 'Surcharge Rate > 50 Lakh', 10, 'percentage', 'Surcharge for income > 50 lakh', 'Finance Act', '2025-04-01'),
('2025-26', 'both', 'SURCHARGE_RATE_1CR', 'Surcharge Rate > 1 Crore', 15, 'percentage', 'Surcharge for income > 1 crore', 'Finance Act', '2025-04-01'),
('2025-26', 'both', 'SURCHARGE_RATE_2CR', 'Surcharge Rate > 2 Crore', 25, 'percentage', 'Surcharge for income > 2 crore', 'Finance Act', '2025-04-01'),

-- Maximum surcharge rates (different for old and new regime)
('2025-26', 'new', 'SURCHARGE_MAX_RATE', 'Maximum Surcharge Rate (New Regime)', 25, 'percentage', 'Maximum surcharge rate capped at 25% for new regime', 'Finance Act', '2025-04-01'),
('2025-26', 'old', 'SURCHARGE_MAX_RATE', 'Maximum Surcharge Rate (Old Regime)', 37, 'percentage', 'Maximum surcharge rate at 37% for old regime (income > 5cr)', 'Finance Act', '2025-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Section 80C/80D/etc limits (Old regime specific)
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2025-26', 'old', 'SECTION_80C_LIMIT', 'Section 80C Deduction Limit', 150000, 'amount', 'Maximum deduction under Section 80C', 'Section 80C', '2025-04-01'),
('2025-26', 'old', 'SECTION_80CCD_NPS_LIMIT', 'Section 80CCD(1B) NPS Limit', 50000, 'amount', 'Additional deduction for NPS under Section 80CCD(1B)', 'Section 80CCD(1B)', '2025-04-01'),
('2025-26', 'old', 'SECTION_80D_SELF_LIMIT', 'Section 80D Self/Family Limit', 25000, 'amount', 'Health insurance premium limit for self and family', 'Section 80D', '2025-04-01'),
('2025-26', 'old', 'SECTION_80D_SELF_SENIOR_LIMIT', 'Section 80D Self/Family Senior Limit', 50000, 'amount', 'Health insurance premium limit for senior citizens (self/family)', 'Section 80D', '2025-04-01'),
('2025-26', 'old', 'SECTION_80D_PARENTS_LIMIT', 'Section 80D Parents Limit', 25000, 'amount', 'Health insurance premium limit for parents', 'Section 80D', '2025-04-01'),
('2025-26', 'old', 'SECTION_80D_PARENTS_SENIOR_LIMIT', 'Section 80D Parents Senior Limit', 50000, 'amount', 'Health insurance premium limit for senior citizen parents', 'Section 80D', '2025-04-01'),
('2025-26', 'old', 'SECTION_80D_PREVENTIVE_LIMIT', 'Section 80D Preventive Checkup Limit', 5000, 'amount', 'Preventive health checkup limit (within overall 80D)', 'Section 80D', '2025-04-01'),
('2025-26', 'old', 'SECTION_24_HOME_LOAN_LIMIT', 'Section 24 Home Loan Interest Limit', 200000, 'amount', 'Maximum deduction for home loan interest', 'Section 24(b)', '2025-04-01'),
('2025-26', 'old', 'SECTION_80TTA_LIMIT', 'Section 80TTA Savings Interest Limit', 10000, 'amount', 'Maximum deduction for savings account interest', 'Section 80TTA', '2025-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;
