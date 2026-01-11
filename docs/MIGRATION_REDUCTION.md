# Migration reduction plan (fresh DB)

This document summarizes the current migration set and a reduction plan for a brand-new database where backward compatibility is not required.

## Current footprint
- Forward migrations: 152
- Down migrations: 81 (DbUp ignores these; safe to remove)
- DDL only: 35, DML only: 29, DDL+DML: 88, views: 3

## Reduction strategy
1. **Create a baseline schema migration** from the final schema (run all migrations on a scratch DB, then `pg_dump --schema-only`).
2. **Consolidate views** into a single `views.sql` using the latest definitions (drop compatibility-only views if the app no longer needs them).
3. **Consolidate reference data** into one or two seed migrations (tax slabs, chart of accounts, posting rules, statutory payment types).
4. **Drop all backfills and fix-ups** that only exist to repair existing data; bake the corrected schema/data into the baseline.
5. **Delete `_down.sql` files** (DbUp does not run them) and re-sequence the remaining scripts.

## Proposed minimal migration set
- `001_baseline_schema.sql` (tables, types, constraints, indexes, triggers, functions)
- `002_baseline_views.sql` (final view definitions only)
- `003_seed_reference_data.sql` (tax slabs, HSN/SAC, statutory payment types, lookup tables)
- `004_seed_accounts_and_rules.sql` (chart of accounts + posting rules + payroll rules)
- Optional: `005_seed_demo_users.sql` (admin/employee users for dev only)

## Caveats
- Some scripts include hard-coded verification blocks (for example, the trial balance migrations); drop those in the baseline schema.
- There are duplicate numeric prefixes (064, 067, 119-123). A new baseline should renumber sequentially to avoid ordering ambiguity.
- Compatibility views (for example, `employee_salary_transactions`) are only removable if the app no longer queries them.

## Migration-by-migration reduction matrix
Actions below are based on filename intent plus a quick DDL/DML scan. Use this as a checklist and confirm any ambiguous scripts before deleting.

| Migration | Category | Reduction action |
| --- | --- | --- |
| `001_init_schema.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `002_assets_and_subscriptions.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `003_add_company_id_to_employees.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `004_assets_cost_and_disposal.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `005_add_currency_to_assets_maintenance_disposal.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `006_add_subscription_cost_tracking.sql` | schema | Fold into baseline schema. |
| `007_create_loan_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `008_add_loan_fields_to_assets.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `009_add_company_id_to_salary_transactions.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `010_add_transaction_type_to_salary_transactions.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `011_remove_unique_constraint_allow_multiple_payments.sql` | schema | Fold into baseline schema. |
| `012_add_amount_in_inr_to_payments.sql` | schema | Fold into baseline schema. |
| `013_create_payroll_schema.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `014_fix_gujarat_pt_slabs.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `015_initialize_payroll_info_for_existing_employees.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `016_create_tax_parameters.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `017_add_employee_compliance_fields.sql` | schema | Fold into baseline schema. |
| `018_add_salary_component_flags.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `019_create_payroll_calculation_lines.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `020_add_fy_2025_26_tax_data.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `021_add_senior_citizen_tax_slabs.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `022_add_esi_eligibility_periods.sql` | schema | Fold into baseline schema. |
| `023_add_contractor_tds_section.sql` | schema | Fold into baseline schema. |
| `024_enhance_payments_for_indian_compliance.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `025_backfill_payments_for_paid_invoices.sql` | backfill | Drop for fresh DB; no legacy data to migrate. |
| `025_fix_backfill_amounts.sql` | backfill | Drop for fresh DB; no legacy data to migrate. |
| `026_create_bank_accounts.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `027_create_bank_transactions.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `028_add_gst_fields_to_companies.sql` | schema | Fold into baseline schema. |
| `029_add_gst_fields_to_customers.sql` | schema | Fold into baseline schema. |
| `030_create_tds_receivable.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `031_add_gst_fields_to_invoice_items.sql` | schema | Fold into baseline schema. |
| `032_add_gst_fields_to_invoices.sql` | schema | Fold into baseline schema. |
| `033_add_gst_fields_to_products.sql` | schema | Fold into baseline schema. |
| `034_create_hsn_sac_codes.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `035_fix_karnataka_pt_slabs_april_2025.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `036_add_february_tax_to_pt_slabs.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `037_add_tax_declaration_rejection_workflow.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `038_create_tax_declaration_history.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `039_add_resigned_status.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `040_create_users_and_refresh_tokens.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `041_create_leave_management_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `042_fix_new_regime_2025_26_budget_2025.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `050_seed_admin_user.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `051_seed_employee_users.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `052_create_employee_salary_transactions_view.sql` | view | Consolidate into final views.sql (keep latest definition only). |
| `053_create_announcements.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `054_create_support_tickets.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `055_create_employee_documents.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `056_add_employee_hierarchy.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `057_create_approval_workflow_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `058_create_asset_requests.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `059_add_pf_calculation_modes.sql` | schema | Fold into baseline schema. |
| `060_create_calculation_rules.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `061_seed_calculation_rules_and_templates.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `062_update_pt_karnataka_2024.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `063_add_super_admin_and_company_assignments.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `064_add_reconciliation_tracking.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `064_create_file_storage.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `065_create_document_categories.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `066_create_expense_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `067_add_expense_reconciliation_tracking.sql` | schema | Fold into baseline schema. |
| `067_payment_allocations.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `068_general_ledger.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `069_seed_chart_of_accounts.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `070_seed_posting_rules.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `071_fix_duplicate_account_codes.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `072_einvoice.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `073_tax_rule_packs.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `074_seed_fy_2025_26.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `075_update_invoice_payment_status_view.sql` | view | Consolidate into final views.sql (keep latest definition only). |
| `076_backfill_payment_allocations.sql` | backfill | Drop for fresh DB; no legacy data to migrate. |
| `077_add_invoice_forex_fields.sql` | schema | Fold into baseline schema. |
| `078_create_forex_transactions.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `079_create_firc_tracking.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `080_create_lut_register.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `081_fix_payment_allocations_forex.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `082_seed_forex_posting_rules.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `083_backfill_forex_transactions.sql` | backfill | Drop for fresh DB; no legacy data to migrate. |
| `084_intercompany_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `085_add_reversal_pairing_to_bank_transactions.sql` | schema | Fold into baseline schema. |
| `086_add_reconciliation_difference_fields.sql` | schema | Fold into baseline schema. |
| `087_add_suspense_account.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `088_add_domestic_posting_rules.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `089_fix_posting_rules_trigger_event.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `090_add_advance_tds_posting_rule.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `091_add_gst_itc_tracking.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `092_add_expense_posting_accounts.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `093_add_attachment_type_column.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `094_hybrid_bank_reconciliation.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `095_backfill_bank_account_ledger_links.sql` | backfill | Drop for fresh DB; no legacy data to migrate. |
| `101_enhance_payroll_accounts.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `102_payroll_journal_linkage.sql` | schema | Fold into baseline schema. |
| `103_statutory_payment_tracking.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `104_seed_payroll_posting_rules.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `105_add_bank_account_to_payments.sql` | schema | Fold into baseline schema. |
| `106_contractor_journal_linkage.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `110_gst_compliance_accounts.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `111_tds_comprehensive_accounts.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `112_tcs_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `113_rcm_transactions.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `114_lower_deduction_certificates.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `115_itc_blocked_tracking.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `116_form_16.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `117_form_24q_filings.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `118_column_388a_other_tds_tcs.sql` | schema | Fold into baseline schema. |
| `119_inventory_base.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `119_vendors.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `120_stock_items.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `120_vendor_invoices.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `121_stock_movements.sql` | schema | Fold into baseline schema. |
| `121_vendor_payments.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `122_stock_transfers.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `122_vendor_posting_rules.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `123_manufacturing_bom.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `123_tags_attribution.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `124_production_orders.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `125_serial_numbers.sql` | schema | Fold into baseline schema. |
| `126_tally_migration_fields.sql` | schema | Fold into baseline schema. |
| `127_tally_migration_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `128_unified_party_model.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `129_recreate_invoice_payment_tables.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `130_fix_parties_column_sizes.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `131_add_missing_tally_columns.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `133_add_indian_accounting_mappings.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `134_tag_driven_tds_system.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `135_contractor_payments_tally_fields.sql` | schema | Fold into baseline schema. |
| `136_statutory_payments_tally_fields.sql` | schema | Fold into baseline schema. |
| `137_statutory_payments_fix_constraints.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `138_bank_transactions_matched_entity.sql` | schema | Fold into baseline schema. |
| `139_contractor_payments_party_id.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `140_bank_transactions_source_voucher_type.sql` | schema | Fold into baseline schema. |
| `141_add_reconciliation_to_payments_and_statutory.sql` | schema | Fold into baseline schema. |
| `142_add_reconciliation_tracking_to_payments.sql` | schema | Fold into baseline schema. |
| `143_vendor_payment_allocations_extend.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `144_add_unallocated_amount_to_payments.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `145_create_credit_notes.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `146_create_gstr3b_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `147_create_gstr2b_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `148_create_advance_tax_tables.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `149_add_ytd_projection_split.sql` | schema | Fold into baseline schema. |
| `150_add_book_taxable_reconciliation.sql` | schema | Fold into baseline schema. |
| `151_add_advance_tax_revisions.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `152_add_mat_credit_register.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `153_create_audit_trail.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `154_seed_posting_rules_for_all_companies.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `155_add_contractor_payment_rules.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `156_add_default_fallback_posting_rules.sql` | data | Move into seed migration (if reference) or drop if not needed. |
| `157_seed_standard_coa_and_fix_posting_rules.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
| `158_trial_balance_computed_from_je_lines.sql` | schema+data | Split: schema into baseline, data into seed (if reference data). |
| `159_fix_trial_balance_view_logic.sql` | view | Consolidate into final views.sql (keep latest definition only). |
| `160_fix_tally_opening_balance_convention.sql` | fix/update | Fold corrected schema/data into baseline; drop standalone migration. |
