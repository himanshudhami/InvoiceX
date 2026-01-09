# Assets Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: Depreciation auto-run pending

---

## Overview

The Assets module manages fixed asset lifecycle from acquisition to disposal, including depreciation calculation, employee assignments, maintenance tracking, and disposal accounting. Supports both straight-line and WDV depreciation methods.

### Key Features
- Asset catalog with categories and models
- Purchase tracking (direct, loan-based)
- Depreciation scheduling (SLM, WDV)
- Employee asset assignments
- Maintenance and repair tracking
- Asset disposal and gain/loss
- Asset request workflow
- GST ITC tracking on capital goods

### Key Entities
- **Assets** - Fixed asset master
- **Asset Categories** - Category classification
- **Asset Models** - Make/model definitions
- **Asset Depreciation** - Depreciation schedule
- **Asset Assignments** - Employee assignments
- **Asset Maintenance** - Repair/maintenance records
- **Asset Disposals** - Disposal records
- **Asset Requests** - Employee asset requests

---

## Database Schema

### assets
Fixed asset master with depreciation settings.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `category_id` | UUID | FK to asset_categories |
| `model_id` | UUID | FK to asset_models |
| **Identification** |
| `asset_type` | VARCHAR | `hardware`, `software`, `furniture`, `vehicle`, `equipment` |
| `status` | VARCHAR | `available`, `assigned`, `maintenance`, `disposed` |
| `asset_tag` | VARCHAR | Asset tag/code |
| `serial_number` | VARCHAR | Serial number |
| `name` | VARCHAR | Asset name |
| `description` | TEXT | Description |
| `location` | VARCHAR | Physical location |
| **Purchase** |
| `vendor` | VARCHAR | Vendor name |
| `purchase_date` | DATE | Purchase date |
| `in_service_date` | DATE | Put into service |
| `warranty_expiration` | DATE | Warranty end |
| `purchase_cost` | NUMERIC | Purchase cost |
| `currency` | VARCHAR | Currency |
| `purchase_type` | VARCHAR | `direct`, `loan`, `lease` |
| `invoice_reference` | VARCHAR | Vendor invoice ref |
| **Loan/Lease** |
| `linked_loan_id` | UUID | FK to loans |
| `down_payment_amount` | NUMERIC | Down payment |
| **GST** |
| `gst_amount` | NUMERIC | GST paid |
| `gst_rate` | NUMERIC | GST rate |
| `itc_eligible` | BOOLEAN | ITC claim eligible |
| `tds_on_interest` | NUMERIC | TDS on loan interest |
| **Depreciation** |
| `depreciation_method` | VARCHAR | `slm`, `wdv` |
| `useful_life_months` | INTEGER | Useful life in months |
| `salvage_value` | NUMERIC | Expected salvage |
| `residual_book_value` | NUMERIC | Current book value |
| `depreciation_start_date` | DATE | Depreciation start |
| **Custom** |
| `custom_properties` | JSONB | Custom fields |
| `notes` | TEXT | Notes |

### asset_categories
Asset classification categories.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Category name |
| `code` | VARCHAR | Category code |
| `asset_type` | VARCHAR | Asset type |
| `is_active` | BOOLEAN | Active flag |
| `notes` | TEXT | Notes |

### asset_depreciation
Period-wise depreciation records.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `asset_id` | UUID | FK to assets |
| `method` | VARCHAR | Depreciation method |
| `period_start` | DATE | Period start date |
| `period_end` | DATE | Period end date |
| `depreciation_amount` | NUMERIC | Period depreciation |
| `accumulated_depreciation` | NUMERIC | Accumulated to date |
| `book_value` | NUMERIC | Closing book value |
| `run_at` | TIMESTAMP | Calculation timestamp |
| `notes` | TEXT | Notes |

### asset_assignments
Employee-to-asset assignments.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `asset_id` | UUID | FK to assets |
| `target_type` | VARCHAR | `employee`, `department`, `location` |
| `employee_id` | UUID | FK to employees |
| `company_id` | UUID | FK to companies |
| `assigned_on` | DATE | Assignment date |
| `returned_on` | DATE | Return date |
| `condition_out` | TEXT | Condition at checkout |
| `condition_in` | TEXT | Condition at return |
| `is_active` | BOOLEAN | Current assignment |
| `notes` | TEXT | Notes |

### asset_disposals
Asset disposal/retirement records.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `asset_id` | UUID | FK to assets |
| `disposed_on` | DATE | Disposal date |
| `method` | VARCHAR | `sold`, `scrapped`, `donated`, `written_off` |
| `proceeds` | NUMERIC | Sale proceeds |
| `disposal_cost` | NUMERIC | Disposal expenses |
| `buyer` | VARCHAR | Buyer name (if sold) |
| `currency` | VARCHAR | Currency |
| `notes` | TEXT | Notes |

### asset_maintenance
Maintenance and repair tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `asset_id` | UUID | FK to assets |
| `title` | VARCHAR | Maintenance title |
| `description` | TEXT | Description |
| `status` | VARCHAR | `open`, `in_progress`, `closed` |
| `opened_at` | TIMESTAMP | Opened timestamp |
| `closed_at` | TIMESTAMP | Closed timestamp |
| `vendor` | VARCHAR | Service vendor |
| `cost` | NUMERIC | Maintenance cost |
| `due_date` | DATE | Due date |
| `currency` | VARCHAR | Currency |
| **Bank Reconciliation** |
| `bank_transaction_id` | UUID | FK to bank_transactions |
| `reconciled_at` | TIMESTAMP | Reconciliation time |
| `reconciled_by` | VARCHAR | Reconciled by |
| `notes` | TEXT | Notes |

### asset_requests
Employee asset request workflow.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `employee_id` | UUID | FK to employees |
| `asset_type` | VARCHAR | Requested asset type |
| `category_id` | UUID | FK to asset_categories |
| `model_id` | UUID | FK to asset_models |
| `justification` | TEXT | Request justification |
| `status` | VARCHAR | `pending`, `approved`, `rejected`, `fulfilled` |
| `requested_at` | TIMESTAMP | Request timestamp |
| `approved_by` | UUID | Approver |
| `approved_at` | TIMESTAMP | Approval time |
| `fulfilled_asset_id` | UUID | FK to assets (assigned) |
| `fulfilled_at` | TIMESTAMP | Fulfillment time |
| `notes` | TEXT | Notes |

---

## Backend Structure

### Controllers
- `WebApi/Controllers/AssetsController.cs`
- `WebApi/Controllers/AssetRequestsController.cs`
- `WebApi/Controllers/Portal/AssetRequestPortalController.cs`

### Entities
- `Core/Entities/Assets/Asset.cs`
- `Core/Entities/Assets/AssetCategory.cs`
- `Core/Entities/Assets/AssetModel.cs`
- `Core/Entities/Assets/AssetDepreciation.cs`
- `Core/Entities/Assets/AssetAssignment.cs`
- `Core/Entities/Assets/AssetDisposal.cs`
- `Core/Entities/Assets/AssetMaintenance.cs`
- `Core/Entities/Assets/AssetRequest.cs`

---

## Frontend Structure

### Pages
- `pages/assets/AssetsManagement.tsx` - Asset catalog
- `pages/assets/AssetRequestsManagement.tsx` - Request management
- `pages/assets/AssetAssignments.tsx` - Assignment tracking

### Services
- `services/api/assets/assetService.ts`
- `services/api/assets/assetRequestService.ts`
- `services/api/assets/assetAssignmentService.ts`

---

## API Endpoints

### Assets
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/assets` | List assets |
| GET | `/api/assets/paged` | Paginated list |
| GET | `/api/assets/{id}` | Get asset |
| POST | `/api/assets` | Create asset |
| PUT | `/api/assets/{id}` | Update asset |
| DELETE | `/api/assets/{id}` | Delete asset |
| POST | `/api/assets/{id}/depreciate` | Run depreciation |
| POST | `/api/assets/{id}/assign` | Assign to employee |
| POST | `/api/assets/{id}/return` | Return from employee |
| POST | `/api/assets/{id}/dispose` | Dispose asset |

### Asset Requests
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/asset-requests` | List requests |
| GET | `/api/asset-requests/paged` | Paginated list |
| POST | `/api/asset-requests` | Create request |
| POST | `/api/asset-requests/{id}/approve` | Approve request |
| POST | `/api/asset-requests/{id}/reject` | Reject request |
| POST | `/api/asset-requests/{id}/fulfill` | Fulfill request |

### Asset Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/asset-categories` | List categories |
| POST | `/api/asset-categories` | Create category |
| PUT | `/api/asset-categories/{id}` | Update category |

---

## Business Rules

### Asset Status Flow
```
available → assigned → returned (available)
                    → maintenance → available
                    → disposed
```

### Depreciation Methods

**Straight Line Method (SLM)**:
```
Annual Depreciation = (Cost - Salvage Value) / Useful Life (years)
```

**Written Down Value (WDV)**:
```
Annual Depreciation = Book Value × Rate
Rate = 1 - (Salvage/Cost)^(1/years)
```

### Income Tax Depreciation Rates (Common)
| Block | Assets | WDV Rate |
|-------|--------|----------|
| 1 | Buildings | 5% / 10% |
| 2 | Furniture, fittings | 10% |
| 3 | Motor vehicles | 15% |
| 4 | Plant & machinery | 15% |
| 5 | Computers, software | 40% |
| 6 | Energy saving devices | 80% |

### Disposal Gain/Loss
```
Gain/Loss = Sale Proceeds - Disposal Cost - Book Value
```

- If positive: Capital Gain
- If negative: Capital Loss

### ITC on Capital Goods
- Capital goods eligible for 100% ITC upfront
- If disposed within 5 years, reverse ITC proportionally
- Blocked categories (e.g., motor vehicles for personal use)

### Asset Request Workflow
```
pending → approved → fulfilled
       → rejected
```

---

## Ledger Integration

### Asset Purchase (Direct)
```
DR: Fixed Asset A/c          Rs. X
DR: GST Input Credit         Rs. Y (if ITC eligible)
    CR: Vendor Payable / Bank  Rs. X+Y
```

### Asset Purchase (Loan)
```
DR: Fixed Asset A/c          Rs. X
DR: GST Input Credit         Rs. Y
    CR: Loan Payable         Rs. X+Y-Down
    CR: Bank (Down Payment)  Rs. Down
```

### Monthly Depreciation
```
DR: Depreciation Expense     Rs. D
    CR: Accumulated Depreciation  Rs. D
```

### Asset Disposal (Sale)
```
DR: Bank / Receivable        Rs. Proceeds
DR: Accumulated Depreciation Rs. Acc Dep
DR: Loss on Disposal         Rs. Loss (if any)
    CR: Fixed Asset A/c      Rs. Cost
    CR: Gain on Disposal     Rs. Gain (if any)
```

---

## Current Gaps / TODO

- [ ] Automatic monthly depreciation run
- [ ] Depreciation schedule report
- [ ] Asset register report (with depreciation)
- [ ] Block-wise depreciation for IT purposes
- [ ] Asset transfer between locations
- [ ] Barcode/QR code generation
- [ ] Physical verification workflow
- [ ] Insurance tracking
- [ ] AMC contract tracking

---

## Related Modules

- [Ledger](05-LEDGER.md) - Depreciation posting
- [Accounts Payable](02-ACCOUNTS-PAYABLE.md) - Asset purchase invoices
- [Banking](04-BANKING.md) - Maintenance payment reconciliation
- [Payroll](06-PAYROLL.md) - Employee assignments

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Asset catalog and assignments operational
- Depreciation calculation implemented
- Asset request workflow functional
