# Manufacturing Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Manufacturing module manages Bill of Materials (BOM) definitions, production orders, material consumption tracking, and serial number generation. Integrates with inventory for stock movements.

### Key Features
- Bill of Materials (BOM) definition
- Production order creation and tracking
- Raw material consumption
- Finished goods receipt
- Serial number tracking for finished goods
- Scrap/wastage accounting

### Key Entities
- **BOM Items** - Bill of Materials components
- **Production Orders** - Manufacturing work orders
- **Production Order Items** - Component consumption tracking
- **Serial Numbers** - Individual unit tracking

---

## Database Schema

### BOM Structure
BOMs are attached to finished good stock items. The `bom_id` in `bom_items` references a stock item that serves as the finished product.

### bom_items
Bill of Materials component lines.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `bom_id` | UUID | FK to stock_items (finished good) |
| `component_id` | UUID | FK to stock_items (raw material) |
| `quantity` | NUMERIC | Quantity per unit of FG |
| `unit_id` | UUID | FK to units_of_measure |
| `scrap_percentage` | NUMERIC | Expected scrap % (default 0) |
| `is_optional` | BOOLEAN | Optional component |
| `sequence` | INTEGER | Display order |
| `notes` | TEXT | Notes |
| `created_at` | TIMESTAMP | Created timestamp |

### production_orders
Manufacturing work orders for producing finished goods.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `order_number` | VARCHAR | Production order number |
| `bom_id` | UUID | FK to stock_items (finished good) |
| `finished_good_id` | UUID | FK to stock_items |
| `warehouse_id` | UUID | FK to warehouses |
| **Quantities** |
| `planned_quantity` | NUMERIC | Planned FG quantity |
| `actual_quantity` | NUMERIC | Actually produced |
| **Dates** |
| `planned_start_date` | DATE | Planned start |
| `planned_end_date` | DATE | Planned end |
| `actual_start_date` | TIMESTAMPTZ | Actual start |
| `actual_end_date` | TIMESTAMPTZ | Actual end |
| **Status** |
| `status` | VARCHAR | `draft`, `released`, `in_progress`, `completed`, `cancelled` |
| `notes` | TEXT | Notes |
| **Workflow** |
| `released_by` | UUID | Released by user |
| `released_at` | TIMESTAMPTZ | Release timestamp |
| `started_by` | UUID | Started by user |
| `started_at` | TIMESTAMPTZ | Start timestamp |
| `completed_by` | UUID | Completed by user |
| `completed_at` | TIMESTAMPTZ | Completion timestamp |
| `cancelled_by` | UUID | Cancelled by user |
| `cancelled_at` | TIMESTAMPTZ | Cancellation timestamp |

### production_order_items
Component consumption tracking per production order.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `production_order_id` | UUID | FK to production_orders |
| `component_id` | UUID | FK to stock_items |
| `planned_quantity` | NUMERIC | Planned consumption |
| `consumed_quantity` | NUMERIC | Actually consumed |
| `unit_id` | UUID | FK to units_of_measure |
| `batch_id` | UUID | FK to stock_batches |
| `warehouse_id` | UUID | Source warehouse |
| `notes` | TEXT | Notes |
| `created_at` | TIMESTAMPTZ | Created timestamp |

### serial_numbers
Individual unit tracking for serialized products.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `stock_item_id` | UUID | FK to stock_items |
| `serial_no` | VARCHAR | Serial number |
| `warehouse_id` | UUID | Current warehouse |
| `batch_id` | UUID | FK to stock_batches |
| `status` | VARCHAR | `available`, `reserved`, `sold`, `returned` |
| `manufacturing_date` | DATE | Manufactured date |
| `warranty_expiry` | DATE | Warranty end date |
| **Production Link** |
| `production_order_id` | UUID | FK to production_orders |
| **Sales Link** |
| `sold_at` | TIMESTAMPTZ | Sale timestamp |
| `sold_invoice_id` | UUID | FK to invoices |
| `notes` | TEXT | Notes |

---

## Backend Structure

### Entities
- `Core/Entities/Manufacturing/BomItem.cs`
- `Core/Entities/Manufacturing/ProductionOrder.cs`
- `Core/Entities/Manufacturing/ProductionOrderItem.cs`

### Controllers
- `WebApi/Controllers/Manufacturing/BomController.cs`
- `WebApi/Controllers/Manufacturing/ProductionOrdersController.cs`

---

## Frontend Structure

### Pages
- `pages/manufacturing/bom/BomManagement.tsx` - BOM definitions
- `pages/manufacturing/production/ProductionOrdersManagement.tsx` - Production orders
- `pages/manufacturing/serial/SerialNumbersManagement.tsx` - Serial tracking

### Services
- `services/api/manufacturing/bomService.ts`
- `services/api/manufacturing/productionOrderService.ts`
- `services/api/manufacturing/serialNumberService.ts`

---

## API Endpoints

### Bill of Materials
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/bom/{stockItemId}` | Get BOM for item |
| POST | `/api/bom` | Add BOM component |
| PUT | `/api/bom/{id}` | Update BOM component |
| DELETE | `/api/bom/{id}` | Delete BOM component |
| POST | `/api/bom/copy` | Copy BOM from another item |

### Production Orders
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/production-orders` | List orders |
| GET | `/api/production-orders/paged` | Paginated list |
| GET | `/api/production-orders/{id}` | Get order |
| POST | `/api/production-orders` | Create order |
| PUT | `/api/production-orders/{id}` | Update order |
| POST | `/api/production-orders/{id}/release` | Release order |
| POST | `/api/production-orders/{id}/start` | Start production |
| POST | `/api/production-orders/{id}/complete` | Complete order |
| POST | `/api/production-orders/{id}/cancel` | Cancel order |

### Serial Numbers
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/serial-numbers` | List serial numbers |
| GET | `/api/serial-numbers/{id}` | Get serial number |
| GET | `/api/serial-numbers/item/{stockItemId}` | Serials for item |
| POST | `/api/serial-numbers` | Create serial |
| POST | `/api/serial-numbers/bulk` | Bulk create serials |

---

## Business Rules

### Production Order Status Flow
```
draft → released → in_progress → completed
                               → cancelled
```

### BOM Calculation
```
Component Requirement = FG Quantity × Component Qty × (1 + Scrap%)
```

### Production Process
1. **Create Order**: Select finished good, quantity, warehouse
2. **Release**: Lock BOM, calculate material requirements
3. **Start**: Begin production, reserve raw materials
4. **Material Issue**: Consume raw materials (stock-out)
5. **Production Receipt**: Receive finished goods (stock-in)
6. **Complete**: Close order, reconcile variances

### Stock Movements from Production
| Stage | Movement Type | Direction |
|-------|---------------|-----------|
| Material Issue | `production_out` | - (raw material) |
| FG Receipt | `production_in` | + (finished good) |
| Scrap | `adjustment` | - (write-off) |

### Serial Number Lifecycle
```
Created (available) → Reserved → Sold → [Returned → available]
```

### Variance Tracking
- **Quantity Variance**: Actual vs Planned FG output
- **Material Variance**: Actual vs Standard consumption
- **Scrap Variance**: Actual vs Expected scrap

---

## Integration Points

### With Inventory Module
- BOM components linked to stock_items
- Production creates stock_movements
- Serial numbers linked to stock_items

### With Billing Module
- Serial numbers linked to invoices on sale
- Tracks which serial was sold to which customer

### With Ledger Module
- Production orders can post WIP entries
- Material consumption posts to expense
- FG receipt posts to inventory

---

## Current Gaps / TODO

- [ ] Multi-level BOM (sub-assemblies)
- [ ] Routing/operations definition
- [ ] Work center capacity planning
- [ ] Co-products and by-products
- [ ] Production costing and variance reports
- [ ] Quality control checkpoints
- [ ] Batch production with serial generation
- [ ] Production schedule/Gantt view

---

## Related Modules

- [Inventory](07-INVENTORY.md) - Stock items, movements
- [Ledger](05-LEDGER.md) - Cost posting
- [Billing](01-BILLING.md) - Serial number in invoices

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Basic BOM and production order workflow operational
- Serial number tracking implemented
