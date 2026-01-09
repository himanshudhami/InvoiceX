# Inventory Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Inventory module manages stock items, warehouses (godowns), stock movements, and inter-warehouse transfers. Supports batch tracking, multiple valuation methods, and integration with sales/purchase transactions.

### Key Features
- Stock item master with HSN/GST
- Warehouse (godown) hierarchy
- Stock group categorization
- Batch and serial number tracking
- Stock movements with audit trail
- Inter-warehouse transfers
- Valuation methods (Weighted Average, FIFO)
- Reorder level alerts
- Units of measure management

### Key Entities
- **Stock Items** - Inventory item master
- **Stock Groups** - Hierarchical categorization
- **Warehouses** - Storage locations (godowns)
- **Stock Batches** - Batch/lot tracking
- **Stock Movements** - In/out transactions
- **Stock Transfers** - Inter-warehouse movements
- **Units of Measure** - UOM master

---

## Database Schema

### stock_items
Stock item master with pricing and inventory settings.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Item name |
| `sku` | VARCHAR | Stock keeping unit |
| `description` | TEXT | Item description |
| `stock_group_id` | UUID | FK to stock_groups |
| `base_unit_id` | UUID | FK to units_of_measure |
| **Tax** |
| `hsn_sac_code` | VARCHAR | HSN/SAC code |
| `gst_rate` | NUMERIC | GST rate (default 18%) |
| **Opening Stock** |
| `opening_quantity` | NUMERIC | Opening qty (default 0) |
| `opening_value` | NUMERIC | Opening value |
| **Current Stock** |
| `current_quantity` | NUMERIC | Current qty |
| `current_value` | NUMERIC | Current value |
| **Reorder Settings** |
| `reorder_level` | NUMERIC | Reorder trigger level |
| `reorder_quantity` | NUMERIC | Quantity to reorder |
| `minimum_stock` | NUMERIC | Minimum stock level |
| `maximum_stock` | NUMERIC | Maximum stock level |
| **Tracking** |
| `is_batch_enabled` | BOOLEAN | Enable batch tracking |
| `is_serial_enabled` | BOOLEAN | Enable serial tracking |
| **Valuation** |
| `valuation_method` | VARCHAR | `weighted_avg`, `fifo`, `lifo` |
| **Pricing** |
| `cost_price` | NUMERIC | Standard cost |
| `selling_price` | NUMERIC | Standard selling price |
| `mrp` | NUMERIC | Maximum retail price |
| **Tally Migration** |
| `tally_stock_item_guid` | VARCHAR | Tally GUID |
| `tally_stock_item_name` | VARCHAR | Tally name |
| `tally_migration_batch_id` | UUID | Migration batch |
| **Flags** |
| `is_active` | BOOLEAN | Active flag |

### stock_groups
Hierarchical stock categorization (like Tally).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Group name |
| `parent_stock_group_id` | UUID | FK to parent group |
| `tally_stock_group_guid` | VARCHAR | Tally GUID |
| `tally_stock_group_name` | VARCHAR | Tally name |
| `tally_migration_batch_id` | UUID | Migration batch |
| `is_active` | BOOLEAN | Active flag |

### warehouses
Storage locations (godowns) with hierarchy support.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Warehouse name |
| `code` | VARCHAR | Warehouse code |
| `address` | TEXT | Address |
| `city` | VARCHAR | City |
| `state` | VARCHAR | State |
| `pin_code` | VARCHAR | PIN code |
| `is_default` | BOOLEAN | Default warehouse |
| `parent_warehouse_id` | UUID | FK to parent warehouse |
| `tally_godown_guid` | VARCHAR | Tally GUID |
| `tally_godown_name` | VARCHAR | Tally name |
| `tally_migration_batch_id` | UUID | Migration batch |
| `is_active` | BOOLEAN | Active flag |

### stock_batches
Batch/lot tracking for items.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `stock_item_id` | UUID | FK to stock_items |
| `warehouse_id` | UUID | FK to warehouses |
| `batch_number` | VARCHAR | Batch/lot number |
| `manufacturing_date` | DATE | Manufacturing date |
| `expiry_date` | DATE | Expiry date |
| `quantity` | NUMERIC | Current qty in batch |
| `value` | NUMERIC | Current value |
| `cost_rate` | NUMERIC | Cost per unit |
| `tally_batch_guid` | VARCHAR | Tally GUID |
| `is_active` | BOOLEAN | Active flag |

### stock_movements
All stock in/out transactions with audit trail.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `stock_item_id` | UUID | FK to stock_items |
| `warehouse_id` | UUID | FK to warehouses |
| `batch_id` | UUID | FK to stock_batches |
| `movement_date` | DATE | Transaction date |
| `movement_type` | VARCHAR | `in`, `out`, `transfer_in`, `transfer_out`, `adjustment` |
| `quantity` | NUMERIC | Movement quantity |
| `rate` | NUMERIC | Rate per unit |
| `value` | NUMERIC | Total value |
| **Source** |
| `source_type` | VARCHAR | `invoice`, `vendor_invoice`, `transfer`, `adjustment`, `production` |
| `source_id` | UUID | FK to source document |
| `source_number` | VARCHAR | Source document number |
| **Ledger** |
| `journal_entry_id` | UUID | FK to journal_entries |
| **Running Totals** |
| `running_quantity` | NUMERIC | Running qty after movement |
| `running_value` | NUMERIC | Running value after movement |
| **Tally** |
| `tally_voucher_guid` | VARCHAR | Tally GUID |
| `notes` | TEXT | Notes |
| `created_by` | UUID | Created by user |

### stock_transfers
Inter-warehouse stock transfers.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `transfer_number` | VARCHAR | Transfer document number |
| `transfer_date` | DATE | Transfer date |
| `from_warehouse_id` | UUID | Source warehouse |
| `to_warehouse_id` | UUID | Destination warehouse |
| `status` | VARCHAR | `draft`, `approved`, `in_transit`, `completed`, `cancelled` |
| `total_quantity` | NUMERIC | Total qty transferred |
| `total_value` | NUMERIC | Total value |
| `notes` | TEXT | Notes |
| **Workflow** |
| `created_by` | UUID | Created by |
| `approved_by` | UUID | Approved by |
| `approved_at` | TIMESTAMP | Approval time |
| `completed_by` | UUID | Completed by |
| `completed_at` | TIMESTAMP | Completion time |
| **Tally** |
| `tally_voucher_guid` | VARCHAR | Tally GUID |

### stock_transfer_items
Line items for stock transfers.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `stock_transfer_id` | UUID | FK to stock_transfers |
| `stock_item_id` | UUID | FK to stock_items |
| `batch_id` | UUID | FK to stock_batches |
| `quantity` | NUMERIC | Quantity to transfer |
| `rate` | NUMERIC | Transfer rate |
| `value` | NUMERIC | Line value |
| `received_quantity` | NUMERIC | Actually received (for partial) |
| `notes` | TEXT | Line notes |

### units_of_measure
Unit of measure master.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies (null for system units) |
| `name` | VARCHAR | Unit name (Pieces, Kilograms) |
| `symbol` | VARCHAR | Symbol (Pcs, Kg) |
| `decimal_places` | INTEGER | Decimal precision |
| `is_system_unit` | BOOLEAN | System-defined unit |
| `tally_unit_guid` | VARCHAR | Tally GUID |
| `tally_unit_name` | VARCHAR | Tally name |
| `tally_migration_batch_id` | UUID | Migration batch |

---

## Backend Structure

### Entities
- `Core/Entities/Inventory/StockItem.cs`
- `Core/Entities/Inventory/StockGroup.cs`
- `Core/Entities/Inventory/Warehouse.cs`
- `Core/Entities/Inventory/StockBatch.cs`
- `Core/Entities/Inventory/StockMovement.cs`
- `Core/Entities/Inventory/StockTransfer.cs`
- `Core/Entities/Inventory/StockTransferItem.cs`

### Repositories
- `Infrastructure/Data/Inventory/StockItemRepository.cs`
- `Infrastructure/Data/Inventory/StockGroupRepository.cs`
- `Infrastructure/Data/Inventory/StockBatchRepository.cs`
- `Infrastructure/Data/Inventory/StockMovementRepository.cs`
- `Infrastructure/Data/Inventory/StockTransferRepository.cs`
- `Infrastructure/Data/Inventory/StockTransferItemRepository.cs`

### Controllers
- `WebApi/Controllers/Inventory/StockItemsController.cs`
- `WebApi/Controllers/Inventory/StockGroupsController.cs`
- `WebApi/Controllers/Inventory/WarehousesController.cs`
- `WebApi/Controllers/Inventory/StockMovementsController.cs`
- `WebApi/Controllers/Inventory/StockTransfersController.cs`

---

## Frontend Structure

### Pages
- `pages/inventory/items/StockItemsManagement.tsx` - Stock items list
- `pages/inventory/stockgroups/StockGroupsManagement.tsx` - Stock groups
- `pages/inventory/warehouses/WarehousesManagement.tsx` - Warehouses
- `pages/inventory/movements/StockMovementsManagement.tsx` - Movement history
- `pages/inventory/transfers/StockTransfersManagement.tsx` - Stock transfers
- `pages/inventory/units/UnitsOfMeasureManagement.tsx` - UOM management

### Services
- `services/api/inventory/stockItemService.ts`
- `services/api/inventory/stockGroupService.ts`
- `services/api/inventory/warehouseService.ts`
- `services/api/inventory/stockMovementService.ts`
- `services/api/inventory/stockTransferService.ts`

---

## API Endpoints

### Stock Items
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/stock-items` | List items |
| GET | `/api/stock-items/paged` | Paginated list |
| GET | `/api/stock-items/{id}` | Get item |
| POST | `/api/stock-items` | Create item |
| PUT | `/api/stock-items/{id}` | Update item |
| DELETE | `/api/stock-items/{id}` | Delete item |

### Stock Groups
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/stock-groups` | List groups |
| GET | `/api/stock-groups/{id}` | Get group |
| POST | `/api/stock-groups` | Create group |
| PUT | `/api/stock-groups/{id}` | Update group |

### Warehouses
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/warehouses` | List warehouses |
| GET | `/api/warehouses/{id}` | Get warehouse |
| POST | `/api/warehouses` | Create warehouse |
| PUT | `/api/warehouses/{id}` | Update warehouse |

### Stock Movements
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/stock-movements` | List movements |
| GET | `/api/stock-movements/paged` | Paginated with filters |
| GET | `/api/stock-movements/item/{itemId}` | Movements for item |
| POST | `/api/stock-movements/adjustment` | Create adjustment |

### Stock Transfers
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/stock-transfers` | List transfers |
| GET | `/api/stock-transfers/{id}` | Get transfer |
| POST | `/api/stock-transfers` | Create transfer |
| PUT | `/api/stock-transfers/{id}` | Update transfer |
| POST | `/api/stock-transfers/{id}/approve` | Approve transfer |
| POST | `/api/stock-transfers/{id}/complete` | Complete transfer |

---

## Business Rules

### Movement Types
| Type | Direction | Description |
|------|-----------|-------------|
| `in` | + | Purchase/receipt |
| `out` | - | Sales/consumption |
| `transfer_in` | + | Received from transfer |
| `transfer_out` | - | Sent via transfer |
| `adjustment` | +/- | Stock adjustment |
| `production_in` | + | Production output |
| `production_out` | - | Raw material consumption |

### Valuation Methods
| Method | Description |
|--------|-------------|
| `weighted_avg` | Weighted average cost (default) |
| `fifo` | First In First Out |
| `lifo` | Last In First Out |

### Stock Transfer Flow
```
draft → approved → in_transit → completed
                             → cancelled
```

### Batch Tracking
- Items with `is_batch_enabled = true` require batch selection
- Batches track manufacturing and expiry dates
- FEFO (First Expiry First Out) for batch consumption

### Running Balance Calculation
- `running_quantity` maintained on each movement
- `running_value` for valuation audit trail
- Current stock = sum of movements or last `running_quantity`

### Integration Points
- **Sales (Invoices)**: Auto stock-out on invoice approval
- **Purchases (Vendor Invoices)**: Auto stock-in on GRN
- **Manufacturing**: BOM consumption and finished goods receipt
- **Ledger**: Stock valuation posting

### Reorder Alerts
- Alert when `current_quantity <= reorder_level`
- Suggested order = `reorder_quantity`
- Max stock check prevents over-ordering

---

## Current Gaps / TODO

- [ ] Serial number tracking
- [ ] Multi-unit conversion (alternate units)
- [ ] Stock aging report
- [ ] Warehouse-wise stock report
- [ ] Physical stock count / adjustment workflow
- [ ] Negative stock prevention (configurable)
- [ ] Batch expiry alerts
- [ ] Min/Max stock alerts dashboard

---

## Related Modules

- [Billing](01-BILLING.md) - Stock-out on sales
- [Accounts Payable](02-ACCOUNTS-PAYABLE.md) - Stock-in on purchase
- [Manufacturing](08-MANUFACTURING.md) - BOM consumption
- [Ledger](05-LEDGER.md) - Stock valuation posting

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Core inventory workflow operational
- Tally migration support for stock items, groups, warehouses
