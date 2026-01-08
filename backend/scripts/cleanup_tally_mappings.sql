-- Cleanup script for duplicate/incorrect tally_field_mappings
-- Run this AFTER migrations to fix mapping issues

-- ============================================
-- 1. Remove duplicate mappings with NULL tally_name
--    Keep only the '' (empty string) entries from migration 133
-- ============================================
DELETE FROM tally_field_mappings
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
AND tally_name IS NULL;

-- Verify cleanup
SELECT 'After NULL cleanup' as step, COUNT(*) as remaining FROM tally_field_mappings WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2';

-- ============================================
-- 2. Fix any incorrect 'suspense' mappings
-- ============================================

-- CONSULTANTS should map to vendors, not suspense
UPDATE tally_field_mappings
SET target_entity = 'vendors'
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
AND tally_group_name IN ('CONSULTANTS', 'Consultants')
AND target_entity = 'suspense';

-- Check what's still mapped to suspense (should only be legitimate suspense accounts)
SELECT 'Suspense mappings after fix' as step, tally_group_name, target_entity
FROM tally_field_mappings
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
AND target_entity = 'suspense';

-- ============================================
-- 3. Final verification
-- ============================================
SELECT
  'Final counts' as step,
  COUNT(*) as total_mappings,
  COUNT(*) FILTER (WHERE tally_name IS NULL) as null_tally_name,
  COUNT(*) FILTER (WHERE tally_name = '') as empty_tally_name,
  COUNT(*) FILTER (WHERE target_entity = 'suspense') as suspense_target,
  COUNT(*) FILTER (WHERE target_entity = 'vendors') as vendor_target
FROM tally_field_mappings
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2';
