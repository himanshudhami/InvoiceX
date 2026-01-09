# Administration Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: Role-based permissions granularity pending

---

## Overview

The Administration module handles system-wide configuration: companies, users, roles, permissions, Tally data migration, and system settings. Core to multi-company operations and user access management.

### Key Features
- Multi-company management
- User management with roles
- Multi-company user assignments
- Tally ERP data migration
- Company settings and branding
- System configuration
- Audit logging

### Key Entities
- **Companies** - Company master with GST/branding
- **Users** - User accounts with authentication
- **User Company Assignments** - Multi-company access
- **Tally Migration Batches** - Data migration tracking

---

## Database Schema

### companies
Company master with statutory and branding info.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `name` | VARCHAR | Company name |
| `logo_url` | TEXT | Logo URL |
| **Address** |
| `address_line1` | VARCHAR | Address line 1 |
| `address_line2` | VARCHAR | Address line 2 |
| `city` | VARCHAR | City |
| `state` | VARCHAR | State |
| `zip_code` | VARCHAR | PIN code |
| `country` | VARCHAR | Country |
| **Contact** |
| `email` | VARCHAR | Company email |
| `phone` | VARCHAR | Phone |
| `website` | VARCHAR | Website |
| **Statutory** |
| `gstin` | VARCHAR | GST number |
| `gst_state_code` | VARCHAR | State code |
| `gst_registration_type` | VARCHAR | Registration type |
| `pan_number` | VARCHAR | PAN |
| `cin_number` | VARCHAR | CIN |
| `tax_number` | VARCHAR | Other tax ID |
| **Branding** |
| `payment_instructions` | TEXT | Payment instructions |
| `signature_type` | VARCHAR | `text`, `image`, `draw` |
| `signature_data` | TEXT | Signature data |
| `signature_name` | VARCHAR | Signatory name |
| `signature_font` | VARCHAR | Signature font |
| `signature_color` | VARCHAR | Signature color |
| **Invoice** |
| `invoice_template_id` | UUID | Default template |
| **SOW Settings** |
| `sownumberprefix` | VARCHAR | SOW prefix |
| `sownumbercounter` | INTEGER | SOW counter |
| `sowdefaultterms` | TEXT | Default terms |
| `sowtemplate` | TEXT | SOW template |

### users
User account master.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `email` | VARCHAR | Login email |
| `password_hash` | VARCHAR | Password hash |
| `display_name` | VARCHAR | Display name |
| `role` | VARCHAR | Primary role |
| `company_id` | UUID | Primary company |
| `employee_id` | UUID | FK to employees |
| `is_active` | BOOLEAN | Active flag |
| `is_super_admin` | BOOLEAN | Super admin flag |
| **Security** |
| `last_login_at` | TIMESTAMP | Last login |
| `failed_login_attempts` | INTEGER | Failed attempts |
| `lockout_end_at` | TIMESTAMP | Lockout expiry |
| **Audit** |
| `created_by` | VARCHAR | Created by |
| `updated_by` | VARCHAR | Updated by |

### user_company_assignments
Multi-company access for users.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `user_id` | UUID | FK to users |
| `company_id` | UUID | FK to companies |
| `role` | VARCHAR | Role in this company |
| `is_primary` | BOOLEAN | Primary company flag |
| **Audit** |
| `created_by` | VARCHAR | Created by |
| `updated_by` | VARCHAR | Updated by |

### tally_migration_batches
Tally ERP data migration tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `batch_number` | VARCHAR | Batch identifier |
| `import_type` | VARCHAR | `masters`, `vouchers`, `full` |
| **Source** |
| `source_file_name` | VARCHAR | File name |
| `source_file_size` | BIGINT | File size |
| `source_format` | VARCHAR | `xml`, `json` |
| `source_checksum` | VARCHAR | File checksum |
| **Tally Info** |
| `tally_company_name` | VARCHAR | Tally company |
| `tally_company_guid` | VARCHAR | Tally GUID |
| `tally_from_date` | DATE | Data from |
| `tally_to_date` | DATE | Data to |
| `tally_financial_year` | VARCHAR | FY |
| **Status** |
| `status` | VARCHAR | `pending`, `parsing`, `validating`, `importing`, `completed`, `failed` |
| **Counts - Ledgers** |
| `total_ledgers` | INTEGER | Total |
| `imported_ledgers` | INTEGER | Imported |
| `skipped_ledgers` | INTEGER | Skipped |
| `failed_ledgers` | INTEGER | Failed |
| **Counts - Stock** |
| `total_stock_items` | INTEGER | Total |
| `imported_stock_items` | INTEGER | Imported |
| `skipped_stock_items` | INTEGER | Skipped |
| `failed_stock_items` | INTEGER | Failed |
| `total_stock_groups` | INTEGER | Groups |
| `imported_stock_groups` | INTEGER | Imported groups |
| **Counts - Other** |
| `total_cost_centers` | INTEGER | Cost centers |
| `imported_cost_centers` | INTEGER | Imported |
| `total_godowns` | INTEGER | Godowns |
| `imported_godowns` | INTEGER | Imported |
| `total_units` | INTEGER | Units |
| `imported_units` | INTEGER | Imported |
| **Counts - Vouchers** |
| `total_vouchers` | INTEGER | Total |
| `imported_vouchers` | INTEGER | Imported |
| `skipped_vouchers` | INTEGER | Skipped |
| `failed_vouchers` | INTEGER | Failed |
| `voucher_counts` | JSONB | Type-wise counts |
| **Suspense** |
| `suspense_entries_created` | INTEGER | Suspense count |
| `suspense_total_amount` | NUMERIC | Suspense amount |
| **Timing** |
| `upload_started_at` | TIMESTAMPTZ | Upload start |
| `parsing_started_at` | TIMESTAMPTZ | Parse start |
| `parsing_completed_at` | TIMESTAMPTZ | Parse end |
| `validation_started_at` | TIMESTAMPTZ | Validation start |
| `validation_completed_at` | TIMESTAMPTZ | Validation end |
| `import_started_at` | TIMESTAMPTZ | Import start |
| `import_completed_at` | TIMESTAMPTZ | Import end |
| **Errors** |
| `error_message` | TEXT | Error message |
| `error_details` | JSONB | Error details |
| `mapping_config` | JSONB | Mapping rules |
| `created_by` | UUID | Created by |

---

## Backend Structure

### Controllers
- `WebApi/Controllers/CompaniesController.cs`
- `WebApi/Controllers/Common/CompanyAuthorizedController.cs` (base)

### Entities
- `Core/Entities/Company.cs`
- `Core/Entities/User.cs`
- `Core/Entities/UserCompanyAssignment.cs`
- `Core/Entities/Migration/TallyMigrationBatch.cs`

---

## Frontend Structure

### Pages
- `pages/admin/settings/Settings.tsx` - Company settings
- `pages/settings/migration/TallyMigration.tsx` - Tally import
- `pages/settings/migration/TallyMigrationHistory.tsx` - Import history
- `pages/settings/tags/TagsManagement.tsx` - Tags/labels
- `pages/settings/tags/AttributionRulesManagement.tsx` - Attribution rules

### Services
- `services/api/company/companyService.ts`
- `services/api/user/userService.ts`
- `services/api/migration/tallyMigrationService.ts`

---

## API Endpoints

### Companies
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/companies` | List companies |
| GET | `/api/companies/{id}` | Get company |
| POST | `/api/companies` | Create company |
| PUT | `/api/companies/{id}` | Update company |
| PUT | `/api/companies/{id}/settings` | Update settings |
| PUT | `/api/companies/{id}/branding` | Update branding |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List users |
| GET | `/api/users/{id}` | Get user |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| POST | `/api/users/{id}/deactivate` | Deactivate user |
| POST | `/api/users/{id}/reset-password` | Reset password |
| GET | `/api/users/{id}/companies` | User's companies |
| POST | `/api/users/{id}/companies` | Assign company |
| DELETE | `/api/users/{id}/companies/{companyId}` | Remove company |

### Tally Migration
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tally-migration/batches` | List batches |
| GET | `/api/tally-migration/batches/{id}` | Get batch |
| POST | `/api/tally-migration/upload` | Upload file |
| POST | `/api/tally-migration/batches/{id}/import` | Start import |
| POST | `/api/tally-migration/batches/{id}/cancel` | Cancel import |
| GET | `/api/tally-migration/batches/{id}/logs` | Get logs |

---

## Business Rules

### User Roles
| Role | Description | Permissions |
|------|-------------|-------------|
| `admin` | System administrator | Full access |
| `manager` | Department manager | Approvals, team view |
| `accountant` | Finance team | Finance modules |
| `sales` | Sales team | Billing, customers |
| `hr` | HR team | Employees, payroll |
| `employee` | Regular employee | Self-service only |
| `viewer` | Read-only access | View only |

### Multi-Company Access
- Users can be assigned to multiple companies
- Each assignment has a role (may differ by company)
- Primary company is default on login
- Context switch between assigned companies

### Account Security
- Password hashing with bcrypt
- Account lockout after 5 failed attempts
- Lockout duration: 15 minutes
- Password complexity requirements
- Session management with JWT

### GST Registration Types
| Type | Description |
|------|-------------|
| `regular` | Normal taxpayer |
| `composition` | Composition scheme |
| `unregistered` | Unregistered |
| `sez` | SEZ unit |
| `embassy` | Embassy/consulate |

---

## Tally Migration

### Supported Import Types
| Type | Description |
|------|-------------|
| `masters` | Ledgers, parties, stock items only |
| `vouchers` | Vouchers only (needs masters first) |
| `full` | Complete data migration |

### Migration Process
1. **Upload**: Upload Tally XML export file
2. **Parse**: Parse XML into staging tables
3. **Validate**: Check data integrity, mappings
4. **Map**: Apply account/party mappings
5. **Import**: Insert into production tables
6. **Verify**: Generate variance reports

### Supported Tally Voucher Types
| Voucher | Maps To |
|---------|---------|
| Sales | Invoices |
| Purchase | Vendor Invoices |
| Receipt | Payments |
| Payment | Vendor Payments |
| Contra | Bank Transfers |
| Journal | Journal Entries |

### GUID Preservation
- All imported records retain Tally GUID
- Enables incremental imports
- Supports deduplication

### Suspense Handling
- Unmapped ledgers create suspense entries
- Suspense report for manual resolution
- Post-migration cleanup workflow

---

## Company Settings

### Invoice Settings
- Number prefix and counter
- Default payment terms
- Invoice template selection
- Auto-numbering rules

### Document Templates
- Invoice template HTML/CSS
- SOW template
- Quote template
- Email templates

### Statutory Configuration
- GSTIN and state code
- PAN number
- CIN (for companies)
- TAN (for TDS)

---

## Current Gaps / TODO

- [ ] Granular role-based permissions
- [ ] Permission inheritance
- [ ] Two-factor authentication
- [ ] SSO integration (Google, Microsoft)
- [ ] API key management
- [ ] User activity audit log
- [ ] Automated backup/restore
- [ ] Multi-tenant isolation
- [ ] Email notification templates
- [ ] System health dashboard

---

## Related Modules

- All modules depend on company context
- [Payroll](06-PAYROLL.md) - User-employee linking
- [Approvals](14-APPROVALS.md) - Role-based approvers
- [Ledger](05-LEDGER.md) - Chart of accounts from Tally

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Multi-company architecture operational
- Tally migration functional
- Basic role-based access implemented
