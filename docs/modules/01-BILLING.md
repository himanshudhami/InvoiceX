# Billing Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Billing module handles revenue-side transactions: creating invoices, quotes, receiving customer payments, and managing parties (customers). It integrates with GST compliance (e-invoice, GSTR-1) and export workflows (forex, LUT).

### Key Entities
- **Invoices** - Sales invoices with GST, e-invoice, and export support
- **Quotes** - Quotations that convert to invoices
- **Payments** - Customer receipt tracking with allocation
- **Parties** - Unified customer/vendor master (filter by `is_customer`)
- **Products** - Service/product catalog

---

## Database Schema

### invoices
Primary sales invoice table with Indian GST and export compliance fields.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `party_id` | UUID | FK to parties (customer) |
| `invoice_number` | VARCHAR | Unique invoice number |
| `invoice_date` | DATE | Invoice date |
| `due_date` | DATE | Payment due date |
| `status` | VARCHAR | `draft`, `sent`, `paid`, `overdue`, `cancelled` |
| **Amounts** |
| `subtotal` | NUMERIC | Pre-tax total |
| `tax_amount` | NUMERIC | Total tax |
| `discount_amount` | NUMERIC | Total discounts |
| `total_amount` | NUMERIC | Grand total |
| `paid_amount` | NUMERIC | Amount received |
| `currency` | VARCHAR | Default `INR` |
| **GST Fields** |
| `invoice_type` | VARCHAR | `regular`, `export`, `sez`, `deemed_export` |
| `supply_type` | VARCHAR | `B2B`, `B2C`, `B2CL`, `EXPWP`, `EXPWOP` |
| `place_of_supply` | VARCHAR | State code |
| `reverse_charge` | BOOLEAN | RCM applicable |
| `total_cgst` | NUMERIC | Central GST |
| `total_sgst` | NUMERIC | State GST |
| `total_igst` | NUMERIC | Integrated GST |
| `total_cess` | NUMERIC | GST Cess |
| `sez_category` | VARCHAR | SEZ classification |
| `b2c_large` | BOOLEAN | B2C > 2.5L threshold |
| **E-Invoice Fields** |
| `e_invoice_applicable` | BOOLEAN | E-invoice required |
| `e_invoice_irn` | VARCHAR | Invoice Reference Number |
| `e_invoice_ack_number` | VARCHAR | Acknowledgement number |
| `e_invoice_ack_date` | TIMESTAMP | Acknowledgement date |
| `e_invoice_qr_code` | TEXT | QR code data |
| `e_invoice_signed_json` | TEXT | Signed JSON from NIC |
| `e_invoice_status` | VARCHAR | `not_applicable`, `pending`, `generated`, `cancelled` |
| `e_invoice_cancel_date` | TIMESTAMP | Cancellation date |
| `e_invoice_cancel_reason` | TEXT | Cancellation reason |
| **E-Way Bill Fields** |
| `eway_bill_number` | VARCHAR | E-way bill number |
| `eway_bill_date` | TIMESTAMP | Generation date |
| `eway_bill_valid_until` | TIMESTAMP | Validity |
| **Export Fields** |
| `export_type` | VARCHAR | `with_payment`, `without_payment` |
| `port_code` | VARCHAR | Port of export |
| `shipping_bill_number` | VARCHAR | Shipping bill reference |
| `shipping_bill_date` | DATE | Shipping bill date |
| `export_duty` | NUMERIC | Export duty |
| `foreign_currency` | VARCHAR | Invoice currency (USD, EUR, etc.) |
| `exchange_rate` | NUMERIC | Rate at invoice date |
| `foreign_currency_amount` | NUMERIC | Amount in foreign currency |
| `invoice_exchange_rate` | NUMERIC | Conversion rate used |
| `invoice_amount_inr` | NUMERIC | INR equivalent |
| `lut_number` | VARCHAR | LUT reference |
| `lut_valid_from` | DATE | LUT validity start |
| `lut_valid_to` | DATE | LUT validity end |
| `purpose_code` | VARCHAR | FEMA purpose code |
| `ad_bank_name` | VARCHAR | AD Bank |
| `realization_due_date` | DATE | FEMA realization deadline |
| **Posting** |
| `is_posted` | BOOLEAN | Posted to ledger |
| `posted_journal_id` | UUID | FK to journal_entries |
| `posted_at` | TIMESTAMP | Posting timestamp |
| **Transport** |
| `shipping_address` | TEXT | Delivery address |
| `transporter_name` | VARCHAR | Transport company |
| `vehicle_number` | VARCHAR | Vehicle registration |
| **Tally Migration** |
| `tally_voucher_guid` | VARCHAR | Original Tally GUID |
| `tally_voucher_number` | VARCHAR | Original voucher number |
| `tally_voucher_type` | VARCHAR | Tally voucher type |
| `tally_migration_batch_id` | UUID | Migration batch |

### invoice_items
Line items for invoices with per-line GST breakdown.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `invoice_id` | UUID | FK to invoices |
| `product_id` | UUID | FK to products (optional) |
| `description` | TEXT | Line item description |
| `quantity` | NUMERIC | Quantity |
| `unit_price` | NUMERIC | Unit price |
| `tax_rate` | NUMERIC | Tax percentage |
| `discount_rate` | NUMERIC | Discount percentage |
| `line_total` | NUMERIC | Line total after tax/discount |
| `sort_order` | INTEGER | Display order |
| `hsn_sac_code` | VARCHAR | HSN/SAC code |
| `is_service` | BOOLEAN | Service vs goods |
| `cgst_rate` | NUMERIC | CGST rate |
| `cgst_amount` | NUMERIC | CGST amount |
| `sgst_rate` | NUMERIC | SGST rate |
| `sgst_amount` | NUMERIC | SGST amount |
| `igst_rate` | NUMERIC | IGST rate |
| `igst_amount` | NUMERIC | IGST amount |
| `cess_rate` | NUMERIC | Cess rate |
| `cess_amount` | NUMERIC | Cess amount |

### quotes
Quotations with conversion tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `party_id` | UUID | FK to parties |
| `quote_number` | VARCHAR | Quote reference |
| `quote_date` | DATE | Quote date |
| `valid_until` | DATE | Expiry date |
| `status` | VARCHAR | `draft`, `sent`, `accepted`, `rejected`, `expired` |
| `subtotal`, `tax_amount`, `discount_amount`, `total_amount` | NUMERIC | Amounts |
| `converted_to_invoice_id` | UUID | FK if converted |
| `converted_at` | TIMESTAMP | Conversion timestamp |

### payments
Customer payment receipts.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `party_id` | UUID | FK to parties |
| `invoice_id` | UUID | FK to invoices (optional, use allocations instead) |
| `payment_date` | DATE | Receipt date |
| `amount` | NUMERIC | Payment amount |
| `amount_in_inr` | NUMERIC | INR equivalent |
| `currency` | VARCHAR | Payment currency |
| `payment_method` | VARCHAR | `bank_transfer`, `neft`, `rtgs`, `upi`, `cheque`, `cash` |
| `reference_number` | VARCHAR | Bank reference |
| `payment_type` | VARCHAR | `invoice_payment`, `advance`, `on_account` |
| `income_category` | VARCHAR | For non-invoice income |
| **TDS Receivable** |
| `tds_applicable` | BOOLEAN | TDS deducted by customer |
| `tds_section` | VARCHAR | TDS section |
| `tds_rate` | NUMERIC | TDS rate |
| `tds_amount` | NUMERIC | TDS deducted |
| `gross_amount` | NUMERIC | Gross before TDS |
| `financial_year` | VARCHAR | FY for TDS tracking |

### payment_allocations
Bill-wise allocation of payments to invoices (Tally-style settlement).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `payment_id` | UUID | FK to payments |
| `invoice_id` | UUID | FK to invoices |
| `allocated_amount` | NUMERIC | Amount applied to invoice |
| `currency` | VARCHAR | Allocation currency |
| `amount_in_inr` | NUMERIC | INR equivalent |
| `exchange_rate` | NUMERIC | Rate used |
| `allocation_date` | DATE | Allocation date |
| `allocation_type` | VARCHAR | `against_invoice`, `advance_adjustment` |
| `tds_allocated` | NUMERIC | TDS portion of allocation |

### parties
Unified party master (customer/vendor). Filter by `is_customer = true` for billing.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Party name |
| `display_name` | VARCHAR | Short display name |
| `legal_name` | VARCHAR | Legal entity name |
| `party_code` | VARCHAR | Internal code |
| `is_customer` | BOOLEAN | Customer flag |
| `is_vendor` | BOOLEAN | Vendor flag |
| `is_employee` | BOOLEAN | Employee flag |
| **Contact** |
| `email`, `phone`, `mobile`, `website` | VARCHAR | Contact details |
| `contact_person` | VARCHAR | Primary contact |
| **Address** |
| `address_line1`, `address_line2`, `city`, `state`, `state_code`, `pincode`, `country` | VARCHAR | Address |
| **Tax** |
| `pan_number` | VARCHAR | PAN |
| `gstin` | VARCHAR | GST Number |
| `is_gst_registered` | BOOLEAN | GST registered |
| `gst_state_code` | VARCHAR | GST state code |
| `party_type` | VARCHAR | `company`, `individual`, `sez`, `overseas` |

### products
Product/service catalog.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Product name |
| `description` | TEXT | Description |
| `sku` | VARCHAR | SKU code |
| `category` | VARCHAR | Category |
| `type` | VARCHAR | `product`, `service` |
| `unit_price` | NUMERIC | Default price |
| `unit` | VARCHAR | UOM |
| `tax_rate` | NUMERIC | Default tax rate |
| `hsn_sac_code` | VARCHAR | HSN/SAC code |
| `is_service` | BOOLEAN | Service flag |
| `default_gst_rate` | NUMERIC | Default GST |
| `cess_rate` | NUMERIC | Default cess |

---

## Backend Structure

### Entities
- `Core/Entities/Invoice.cs`
- `Core/Entities/InvoiceItem.cs`
- `Core/Entities/Quote.cs`
- `Core/Entities/QuoteItem.cs`
- `Core/Entities/Payment.cs`
- `Core/Entities/PaymentAllocation.cs`
- `Core/Entities/Party.cs`
- `Core/Entities/Product.cs`

### Repositories
- `Infrastructure/Data/InvoiceRepository.cs`
- `Infrastructure/Data/QuoteRepository.cs`
- `Infrastructure/Data/PaymentRepository.cs`
- `Infrastructure/Data/PartyRepository.cs`
- `Infrastructure/Data/ProductRepository.cs`

### Services
- `Application/Services/InvoiceService.cs`
- `Application/Services/QuoteService.cs`
- `Application/Services/PaymentService.cs`

### Controllers
- `WebApi/Controllers/InvoicesController.cs`
- `WebApi/Controllers/InvoiceItemsController.cs`
- `WebApi/Controllers/InvoiceTemplatesController.cs`
- `WebApi/Controllers/QuotesController.cs`
- `WebApi/Controllers/PaymentsController.cs`
- `WebApi/Controllers/CustomersController.cs`
- `WebApi/Controllers/ProductsController.cs`

---

## Frontend Structure

### Pages
- `pages/invoices/InvoicesPage.tsx` - Invoice list
- `pages/invoices/InvoiceDetailPage.tsx` - Invoice view/edit
- `pages/invoices/CreateInvoicePage.tsx` - New invoice
- `pages/quotes/QuotesPage.tsx` - Quote list
- `pages/customers/CustomersPage.tsx` - Customer list
- `pages/products/ProductsPage.tsx` - Product catalog
- `pages/payments/PaymentsPage.tsx` - Payment receipts

### Features/Hooks
```
features/invoices/hooks/
├── useInvoices.ts
├── useInvoicesPaged.ts
├── useInvoice.ts
├── useCreateInvoice.ts
├── useUpdateInvoice.ts
├── useDeleteInvoice.ts
└── invoiceKeys.ts
```

### Services
- `services/api/billing/invoiceService.ts`
- `services/api/billing/quoteService.ts`
- `services/api/billing/paymentService.ts`
- `services/api/billing/customerService.ts`
- `services/api/billing/productService.ts`

### Forms
- `components/forms/InvoiceForm.tsx`
- `components/forms/InvoiceWithItemsForm.tsx`
- `components/forms/QuoteForm.tsx`
- `components/forms/CustomerForm.tsx`
- `components/forms/ProductForm.tsx`

### Components
- `components/invoice/InvoicePreview.tsx`
- `components/invoice/InvoicePdf.tsx`
- `components/ui/CustomerSelect.tsx`
- `components/ui/ProductSelect.tsx`

---

## API Endpoints

### Invoices
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/invoices` | List all invoices |
| GET | `/api/invoices/paged` | Paginated list with filters |
| GET | `/api/invoices/{id}` | Get invoice by ID |
| POST | `/api/invoices` | Create invoice |
| PUT | `/api/invoices/{id}` | Update invoice |
| DELETE | `/api/invoices/{id}` | Delete invoice |
| POST | `/api/invoices/{id}/duplicate` | Duplicate invoice |
| POST | `/api/invoices/{id}/send` | Mark as sent |
| POST | `/api/invoices/{id}/post` | Post to ledger |

### Quotes
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/quotes` | List quotes |
| GET | `/api/quotes/{id}` | Get quote |
| POST | `/api/quotes` | Create quote |
| PUT | `/api/quotes/{id}` | Update quote |
| POST | `/api/quotes/{id}/convert` | Convert to invoice |

### Payments
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/payments` | List payments |
| GET | `/api/payments/paged` | Paginated payments |
| POST | `/api/payments` | Record payment |
| POST | `/api/payments/{id}/allocate` | Allocate to invoices |

### Customers
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers` | List customers |
| GET | `/api/customers/{id}` | Get customer |
| POST | `/api/customers` | Create customer |
| PUT | `/api/customers/{id}` | Update customer |

---

## Business Rules

### Invoice Status Flow
```
draft → sent → [paid | overdue] → cancelled
```

### GST Calculation
- **Intra-state**: CGST + SGST (split 50/50)
- **Inter-state**: IGST (full rate)
- **Export**: Zero-rated (with LUT) or IGST (with refund)
- **SEZ**: Zero-rated

### E-Invoice Applicability
- Mandatory for B2B invoices > threshold
- Generate IRN before sending
- Cancel within 24 hours only

### Payment Allocation
- Payments can be allocated across multiple invoices
- Advance payments tracked as `on_account`
- TDS receivable tracked separately for Form 26AS matching

### Invoice Posting
- Creates journal entry when posted:
  - DR: Accounts Receivable (party)
  - CR: Revenue accounts
  - CR: GST Output (CGST/SGST/IGST)

---

## Current Gaps / TODO

- [ ] E-way bill generation workflow (UI not exposed)
- [ ] Credit note/debit note handling
- [ ] Recurring invoice automation
- [ ] Invoice approval workflow integration
- [ ] Bulk invoice operations
- [ ] Invoice aging report on dashboard

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Module is fully functional with GST and export support
- E-invoice integration operational via GSP
