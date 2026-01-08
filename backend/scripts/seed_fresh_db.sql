-- Seed script for fresh database with Xcdify Solutions
-- Run this AFTER migrations complete
-- NOTE: No transaction wrapper - each statement runs independently so errors are visible

-- ============================================
-- 1. Insert company
-- ============================================
INSERT INTO companies (
    id, name, logo_url, address_line1, address_line2, city, state, zip_code, country,
    email, phone, website, tax_number, payment_instructions,
    signature_type, signature_data, signature_name,
    sownumbercounter, gst_registration_type,
    created_at, updated_at
) VALUES (
    '43e030dc-d522-49e0-819a-31744383b2e2',
    'Xcdify Solutions Pvt Ltd',
    '',
    '80ft Signal road, Koramangla',
    '',
    'Bangalore',
    'Karnataka',
    '560061',
    'India',
    'info@xcdify.com',
    '',
    '',
    'BLRA12345E',
    'Kindly arrange to remit the amount to Axis Bank Ltd, J P Nagar Branch Bangalore India account.
For the transfer, kindly follow the instructions below as provided by the bank.
U.S. Dollars: Funds must be transferred electronically, with the following information.
Payments should be made
Correspondent Bank: Chase Manhattan Bank
New York
(Payment should be routed either via
CHIPS ABA 0002 OR FED ABA 021000021)
CHIPS UID of AXIS Bank is 340191
SWIFT: CHASUS33
Beneficiary Bank: 001-1-407376
AXIS Bank Ltd
JP Nagar Branch
Bangalore
Swift: AXISINBB333
IFSC Code: UTIB0000333
Ultimate beneficiary: Account No. 912020011392251
(With Axis Bank Ltd. JP Nagar, Bangalore)
Name: Xcdify Solutions Private Limited.
Address: # 362, 1st B Main, 2nd Cross, 7th Block West,
Jayanagar, Bangalore - 560082, Karnataka, India.',
    'uploaded',
    '',
    'Xcdify Solutions',
    1,
    'regular',
    NOW(),
    NOW()
) ON CONFLICT (id) DO UPDATE SET updated_at = NOW();

-- Verify company created
SELECT 'Step 1: Company' as step, id, name FROM companies WHERE id = '43e030dc-d522-49e0-819a-31744383b2e2';

-- ============================================
-- 2. Insert users (skip if already exists)
-- ============================================
INSERT INTO users (
    id, email, password_hash, display_name, role, company_id,
    is_active, created_at, updated_at, created_by
)
SELECT
    gen_random_uuid(),
    'admin@company.com',
    '$2a$11$aGfRAO4nx8UlUzogzP49tO1lTwTzRJyvmrHJiia7a1S3i5.GHFsyO',
    'System Administrator',
    'Admin',
    '43e030dc-d522-49e0-819a-31744383b2e2',
    TRUE,
    NOW(),
    NOW(),
    'system'
WHERE NOT EXISTS (SELECT 1 FROM users WHERE email = 'admin@company.com');

INSERT INTO users (
    id, email, password_hash, display_name, role, company_id,
    is_active, created_at, updated_at, created_by
)
SELECT
    gen_random_uuid(),
    'hr@company.com',
    '$2a$11$aGfRAO4nx8UlUzogzP49tO1lTwTzRJyvmrHJiia7a1S3i5.GHFsyO',
    'HR Manager',
    'HR',
    '43e030dc-d522-49e0-819a-31744383b2e2',
    TRUE,
    NOW(),
    NOW(),
    'system'
WHERE NOT EXISTS (SELECT 1 FROM users WHERE email = 'hr@company.com');

INSERT INTO users (
    id, email, password_hash, display_name, role, company_id,
    is_active, created_at, updated_at, created_by
)
SELECT
    gen_random_uuid(),
    'yogesh.dhami@xcdify.com',
    '$2a$11$aGfRAO4nx8UlUzogzP49tO1lTwTzRJyvmrHJiia7a1S3i5.GHFsyO',
    'Yogesh Dhami',
    'Manager',
    '43e030dc-d522-49e0-819a-31744383b2e2',
    TRUE,
    NOW(),
    NOW(),
    'system'
WHERE NOT EXISTS (SELECT 1 FROM users WHERE email = 'yogesh.dhami@xcdify.com');

-- Verify users created
SELECT 'Step 2: Users' as step, email, role FROM users WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2';

-- ============================================
-- 3. Seed Tally default mappings
-- ============================================
DO $$
BEGIN
    PERFORM seed_tally_default_mappings('43e030dc-d522-49e0-819a-31744383b2e2');
    RAISE NOTICE 'Step 3: Tally mappings seeded successfully';
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Step 3 ERROR: %', SQLERRM;
END $$;

-- Verify mappings
SELECT 'Step 3: Tally Mappings' as step, COUNT(*) as count FROM tally_field_mappings WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2';

-- ============================================
-- 4. Seed TDS system (tags + rules)
-- ============================================
DO $$
BEGIN
    PERFORM seed_tds_system('43e030dc-d522-49e0-819a-31744383b2e2');
    RAISE NOTICE 'Step 4: TDS system seeded successfully';
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Step 4 ERROR: %', SQLERRM;
END $$;

-- Verify TDS
SELECT 'Step 4a: TDS Tags' as step, COUNT(*) as count FROM tags WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2' AND tag_group = 'tds_section';
SELECT 'Step 4b: TDS Rules' as step, COUNT(*) as count FROM tds_tag_rules WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2';

-- ============================================
-- 5. Create Tally suspense accounts
-- ============================================
DO $$
BEGIN
    PERFORM create_tally_suspense_accounts('43e030dc-d522-49e0-819a-31744383b2e2');
    RAISE NOTICE 'Step 5: Suspense accounts created successfully';
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Step 5 ERROR: %', SQLERRM;
END $$;

-- Verify suspense accounts
SELECT 'Step 5: Suspense Accounts' as step, COUNT(*) as count FROM chart_of_accounts WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2' AND account_code LIKE '999%';

-- ============================================
-- Final Summary
-- ============================================
SELECT '=== SEED COMPLETE ===' as status;
SELECT 'Company' as entity, name as value FROM companies WHERE id = '43e030dc-d522-49e0-819a-31744383b2e2'
UNION ALL
SELECT 'Users', COUNT(*)::text FROM users WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
UNION ALL
SELECT 'Tally Mappings', COUNT(*)::text FROM tally_field_mappings WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
UNION ALL
SELECT 'TDS Tags', COUNT(*)::text FROM tags WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2' AND tag_group = 'tds_section'
UNION ALL
SELECT 'TDS Rules', COUNT(*)::text FROM tds_tag_rules WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
UNION ALL
SELECT 'Suspense Accounts', COUNT(*)::text FROM chart_of_accounts WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2';
