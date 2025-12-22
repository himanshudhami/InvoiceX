-- Fix: Update trigger_event to match AutoPostingService expectation
-- AutoPostingService uses 'on_finalize' as the default trigger

UPDATE posting_rules
SET trigger_event = 'on_finalize'
WHERE rule_code IN (
    'INV_DOMESTIC_INTRA',
    'INV_DOMESTIC_INTER',
    'PMT_DOMESTIC',
    'PMT_DOMESTIC_TDS',
    'PMT_ADVANCE'
);

-- Verify the update
DO $$
DECLARE
    v_count integer;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM posting_rules
    WHERE rule_code IN ('INV_DOMESTIC_INTRA', 'INV_DOMESTIC_INTER', 'PMT_DOMESTIC', 'PMT_DOMESTIC_TDS', 'PMT_ADVANCE')
    AND trigger_event = 'on_finalize';

    RAISE NOTICE 'Updated % posting rules to use on_finalize trigger', v_count;
END $$;
