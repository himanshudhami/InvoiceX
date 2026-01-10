# Tally Import Logic

This document describes how Tally voucher data is mapped into the application during import.
It is intended for debugging and data validation when working with Tally XML exports.

## Scope

- **Source**: Tally XML exports (e.g. `Transactions 1.xml`, `Master.xml`)
- **Parser**: `TallyXmlParserService.cs` - handles UTF-8 and UTF-16 encoded files
- **Entry Point**: `TallyVoucherMappingService.ImportVouchersAsync`

## Target Tables

| Voucher Type | Target Table | Mapper |
|-------------|--------------|--------|
| Sales | `invoices` | `TallyVoucherMappingService.ImportSalesVouchersAsync` |
| Purchase | `vendor_invoices` | `TallyVoucherMappingService.ImportPurchaseVouchersAsync` |
| Receipt | `payments` | `TallyVoucherMappingService.ImportReceiptVouchersAsync` |
| Payment (Vendor) | `vendor_payments` | `TallyVoucherMappingService.ImportVendorPayment` |
| Payment (Contractor) | `contractor_payments` | `TallyContractorPaymentMapper` |
| Payment (Statutory) | `statutory_payments` | `TallyStatutoryPaymentMapper` |
| Payment (Other) | `journal_entries` | Fallback to journal import |
| Journal/Contra | `journal_entries` | `TallyVoucherMappingService.ImportJournalVouchersAsync` |
| Credit Note | `invoices` (type=credit_note) | `TallyVoucherMappingService.ImportCreditNotesAsync` |
| Debit Note | `vendor_invoices` (type=debit_note) | `TallyVoucherMappingService.ImportDebitNotesAsync` |

All voucher types also create corresponding `bank_transactions` records for reconciliation.

---

## XML Structure

### Envelope Format
```xml
<ENVELOPE>
  <BODY>
    <IMPORTDATA>
      <REQUESTDESC>
        <STATICVARIABLES>
          <SVCURRENTCOMPANY>Company Name</SVCURRENTCOMPANY>
        </STATICVARIABLES>
      </REQUESTDESC>
      <REQUESTDATA>
        <TALLYMESSAGE>
          <VOUCHER>...</VOUCHER>
          <LEDGER>...</LEDGER>
        </TALLYMESSAGE>
      </REQUESTDATA>
    </IMPORTDATA>
  </BODY>
</ENVELOPE>
```

### Key XML Elements

| XML Element | DTO Property | Description |
|-------------|--------------|-------------|
| `GUID` | `Guid` | Unique Tally identifier for deduplication |
| `VOUCHERNUMBER` | `VoucherNumber` | Voucher number |
| `VOUCHERTYPENAME` | `VoucherType` | Sales, Purchase, Receipt, Payment, etc. |
| `DATE` | `Date` | Voucher date (YYYYMMDD format) |
| `PARTYLEDGERNAME` | `PartyLedgerName` | Customer/vendor name |
| `NARRATION` | `Narration` | Description/notes |
| `AMOUNT` | `Amount` | Total voucher amount |
| `PLACEOFSUPPLY` | `PlaceOfSupply` | GST place of supply |
| `ISREVERSECHARGE` | `IsReverseCharge` | Reverse charge flag |
| `PARTYGSTIN` | `GstinOfParty` | Party GSTIN |
| `EINVOICEIRN` | `EInvoiceIrn` | E-Invoice IRN |
| `EWAYBILLNUMBER` | `EWayBillNumber` | E-Way bill number |

### Ledger Entries
```xml
<ALLLEDGERENTRIES.LIST>
  <LEDGERNAME>Axis Bank</LEDGERNAME>
  <AMOUNT>-50000</AMOUNT>
  <BILLALLOCATIONS.LIST>
    <NAME>INV-001</NAME>
    <BILLTYPE>Agst Ref</BILLTYPE>
    <AMOUNT>50000</AMOUNT>
  </BILLALLOCATIONS.LIST>
</ALLLEDGERENTRIES.LIST>
```

**Amount Convention**: Positive = Debit, Negative = Credit (Tally convention)

### Bill Allocations
```xml
<BILLALLOCATIONS.LIST>
  <NAME>Invoice Reference</NAME>
  <BILLTYPE>Agst Ref</BILLTYPE>  <!-- or "New Ref", "Advance", "On Account" -->
  <AMOUNT>50000</AMOUNT>
  <BILLDATE>20250101</BILLDATE>
</BILLALLOCATIONS.LIST>
```

---

## Receipt Vouchers -> `payments`

**Import Method**: `TallyVoucherMappingService.ImportReceiptVouchersAsync`

### Field Mapping

| Tally Field | payments Column | Logic |
|-------------|-----------------|-------|
| `PARTYLEDGERNAME` | `party_id` | Lookup in `parties` by name or Tally GUID |
| `DATE` | `payment_date` | Direct mapping |
| `AMOUNT` | `amount` | Absolute value |
| `NARRATION` (cleaned) | `description` | Newlines replaced with spaces |
| `NARRATION` | `notes` | Raw as-is |
| `VOUCHERNUMBER` | `reference_number` | Falls back to `REFERENCENUMBER` |
| Ledger analysis | `payment_method` | Inferred from ledger names (cash/bank/upi) |
| Bill allocations | `payment_type` | See below |
| `GUID` | `tally_voucher_guid` | For deduplication |
| `VOUCHERNUMBER` | `tally_voucher_number` | For reference |

### Payment Type Determination

Determined by `DetermineReceiptPaymentType()` using bill allocations:

| Bill Allocation Type | payment_type |
|---------------------|--------------|
| `Agst Ref` or `Against Reference` | `invoice_payment` |
| `New Ref`, `Advance`, `On Account` | `advance_received` |
| (none/other) | `direct_income` |

### Bank Transaction

A **credit** `bank_transactions` record is created for reconciliation:
- `transaction_type` = `credit` (money coming IN)
- `matched_entity_type` = `payments`
- `matched_entity_id` = payment ID

---

## Payment Vouchers (Outgoing)

Payment vouchers are first classified using `TallyPaymentClassifier`, then routed to appropriate mappers.

### Classification Logic

**Order of checks** (in `TallyPaymentClassifier.ClassifyAsync`):

1. **Statutory Government Remittance** - Narration contains:
   - `tin 2.0`, `cbdt tax payment` (TDS deposit)
   - `epfo payment`, `epf` + `inb/` or `trrn` (EPF deposit)
   - `esic`, `esi` + `challan` (ESI deposit)
   - `gok e-khajane`, `e-khajane` (Professional Tax)

2. **Party TallyGroupName Lookup** - If party found in `parties`:
   - Contractor groups: `CONSULTANTS`, `Contractors`, `Professional Services` -> `Contractor`
   - Vendor groups: `Sundry Creditors`, `Trade Payables` -> `Vendor`

3. **Salary Payment** - Narration contains `salary` or ledger contains `salary payable`

4. **Loan/EMI Payment** - Narration contains `emi` or `pcr0009`

5. **Bank Charges** - Narration contains `service ch`, `gst @18%`, `bank charge`

6. **Internal Transfer** - Narration contains `ift`, `tparty transfer`

7. **Party.IsVendor Fallback** - If party has `is_vendor = true`

8. **Default** -> `Other` (imported as journal entry)

### Vendor Payments -> `vendor_payments`

| Tally Field | vendor_payments Column |
|-------------|------------------------|
| `PARTYLEDGERNAME` | `party_id` (resolved) |
| `DATE` | `payment_date` |
| `AMOUNT` | `amount` |
| TDS ledger entry | `tds_amount` |
| `VOUCHERNUMBER` | `reference_number` |
| `NARRATION` | `notes` |
| Ledger analysis | `payment_method` |
| `GUID` | `tally_voucher_guid` |

### Contractor Payments -> `contractor_payments`

**Mapper**: `TallyContractorPaymentMapper`

| Tally Field | contractor_payments Column | Logic |
|-------------|---------------------------|-------|
| Classification.PartyId | `party_id` | Must exist (from ledger import) |
| `DATE` | `payment_date` | As DateTime |
| `DATE` | `payment_month`, `payment_year` | Month and year extracted |
| Amount + TDS | `gross_amount` | Net + TDS |
| TDS ledger | `tds_amount`, `tds_rate`, `tds_section` | Extracted from ledger entries |
| Party.PAN | `contractor_pan` | From party record |
| Amount | `net_payable` | Classification amount |
| `NARRATION` | `description` | Direct |
| `GUID` | `tally_voucher_guid` | Deduplication |

**TDS Section Detection**:
- Rate >= 9% -> `194J` (Professional services - 10%)
- Rate >= 1.5% -> `194C` (Contractors individual - 2%)
- Rate > 0% -> `194C` (Contractors others - 1%)
- Default -> `194J`

### Statutory Payments -> `statutory_payments`

**Mapper**: `TallyStatutoryPaymentMapper`

| Tally Field | statutory_payments Column | Logic |
|-------------|--------------------------|-------|
| Narration/Ledger analysis | `payment_type` | PF, ESI, TDS_192, TDS_194C, TDS_194J, PT_KA |
| `DATE` - 1 month | `period_month`, `period_year` | Previous month |
| `DATE` | `payment_date` | Actual payment date |
| Amount | `principal_amount`, `total_amount` | Classification amount |
| Narration | `bank_reference`, `trrn`, `challan_number` | Regex extracted |
| Financial year | `financial_year` | Apr-Mar format (e.g., "2024-25") |
| Calculated | `quarter` | Q1=Apr-Jun, Q2=Jul-Sep, Q3=Oct-Dec, Q4=Jan-Mar |
| `GUID` | `tally_voucher_guid` | Deduplication |

**Payment Type Detection** (from narration and ledger names):
- `epf`, `provident fund` -> `PF`
- `esi`, `employee state insurance` -> `ESI`
- `tds on salary`, `192` -> `TDS_192`
- `gok e-khajane`, `professional tax` -> `PT_KA`
- `tds` + `consulting`/`194j` -> `TDS_194J`
- `tds`, `cbdt`, `tin 2.0` (default) -> `TDS_194C`

### Other Payment Types -> `journal_entries`

- Salary, Loan/EMI, Bank Charges, Internal Transfer, Other
- Created as journal entries with full ledger entry lines
- Bank transaction still created for reconciliation

---

## Sales Vouchers -> `invoices`

**Import Method**: `TallyVoucherMappingService.ImportSalesVouchersAsync`

| Tally Field | invoices Column | Logic |
|-------------|-----------------|-------|
| `PARTYLEDGERNAME` | `party_id` | Customer lookup |
| `VOUCHERNUMBER` | `invoice_number` | Direct |
| `DATE` | `invoice_date` | Direct |
| Bill allocation due date | `due_date` | Or +30 days default |
| Ledger GST amounts | `tax_amount` | CGST + SGST + IGST + Cess |
| Amount - tax | `subtotal` | Calculated |
| `AMOUNT` | `total_amount` | Direct |
| `ISCANCELLED` | `status` | `cancelled` or `paid` |
| `NARRATION` | `notes` | Direct |
| `REFERENCENUMBER` | `po_number` | Direct |
| `PLACEOFSUPPLY` | `place_of_supply` | Converted to state code |
| `EINVOICEIRN` | `einvoice_irn` | Direct |
| `EWAYBILLNUMBER` | `eway_bill_number` | Direct |
| `GUID` | `tally_voucher_guid` | Deduplication |

---

## Purchase Vouchers -> `vendor_invoices`

**Import Method**: `TallyVoucherMappingService.ImportPurchaseVouchersAsync`

| Tally Field | vendor_invoices Column | Logic |
|-------------|------------------------|-------|
| `PARTYLEDGERNAME` | `party_id` | Vendor lookup (creates UNKNOWN if not found) |
| `VOUCHERNUMBER` | `invoice_number` | Direct |
| `DATE` | `invoice_date` | Direct |
| Bill allocation | `due_date` | Or +30 days |
| Ledger GST | `tax_amount` | CGST + SGST + IGST + Cess |
| Ledger TDS | `tds_amount` | Sum of TDS entries |
| `AMOUNT` | `total_amount` | Direct |
| `ISCANCELLED` | `status` | `cancelled` or `approved` |
| `ISREVERSECHARGE` | `reverse_charge` | Direct |
| `GUID` | `tally_voucher_guid` | Deduplication |

---

## Bank Transactions

**Mapper**: `TallyBankTransactionMapper`

### Creation Triggers

| Voucher Type | Transaction Type | Description |
|--------------|------------------|-------------|
| Receipt | `credit` | Money coming IN |
| Payment | `debit` | Money going OUT |
| Contra | `debit` + `credit` | Two transactions (source & destination) |
| Journal (with bank) | Per entry | Based on entry amount sign |

### Field Mapping

| Source | bank_transactions Column |
|--------|-------------------------|
| Voucher date | `transaction_date`, `value_date` |
| Bank ledger entry | `bank_account_id` (resolved) |
| `[VoucherType] | To: Payee | Narration` | `description` |
| Extracted from narration | `reference_number` (NEFT/RTGS/IMPS/UPI ref) |
| Extracted from narration | `cheque_number` |
| Voucher type | `transaction_type` (credit/debit) |
| Abs(amount) | `amount` |
| Entity type | `category` (vendor_payment, contractor, tax, transfer, other) |
| Always false | `is_reconciled` (reconciled later against statement) |
| `tally_import` | `import_source` |
| Voucher type | `source_voucher_type` |
| Entity table | `matched_entity_type` |
| Entity ID | `matched_entity_id` |
| Voucher GUID | `tally_voucher_guid` |

### Reference Number Extraction

Parsed from narration using regex:
- `INB/NEFT/xxx` -> NEFT reference
- `NEFT-xxx`, `RTGS-xxx` -> Direct reference
- `IMPS-xxx` -> IMPS reference
- `UPI-xxx` -> UPI reference
- `CHQ/xxx` -> Cheque number
- `PCRxxx_EMI` -> EMI reference

### Bank Account Resolution

1. Try `PartyLedgerName` -> `bank_accounts.account_name`
2. Try `PartyLedgerGuid` -> `bank_accounts.tally_guid`
3. Scan ledger entries for bank-like names (contains: bank, axis, hdfc, icici, sbi, kotak, etc.)
4. Partial name match fallback

---

## Journal Entries

**Import Method**: `TallyVoucherMappingService.ImportJournalVouchersAsync`

All voucher imports also create corresponding `journal_entries` for GL posting.

### journal_entries

| Tally Field | Column | Logic |
|-------------|--------|-------|
| `JE-{VoucherNumber}` | `journal_number` | Generated |
| `DATE` | `journal_date` | Direct |
| `DATE.Month` | `period_month` | Extracted |
| `NARRATION` | `description` | Or default description |
| `REFERENCENUMBER` | `source_number` | Direct |
| Source table | `source_type` | invoices, vendor_invoices, payments, etc. |
| Source ID | `source_id` | FK to source record |
| `ISCANCELLED` | `status` | `cancelled` or `posted` |
| `GUID` | `tally_voucher_guid` | Deduplication |

### journal_entry_lines

Each ledger entry becomes a line:

| Tally Field | Column | Logic |
|-------------|--------|-------|
| Sequential | `line_number` | 1, 2, 3... |
| Ledger name -> CoA | `account_id` | Resolved or suspense |
| Ledger name | `description` | Direct |
| Amount > 0 | `debit_amount` | Positive entries |
| Amount < 0 | `credit_amount` | Abs of negative entries |

**Unmapped Ledgers**: If ledger not found in `chart_of_accounts`, uses `SUSPENSE-IMPORT` account.

---

## Cost Center / Tags

Cost center allocations in Tally are mapped to `transaction_tags`:

```xml
<CATEGORYALLOCATIONS.LIST>
  <CATEGORY>Project</CATEGORY>
  <COSTCENTREALLOCATIONS.LIST>
    <NAME>Project Alpha</NAME>
    <AMOUNT>50000</AMOUNT>
  </COSTCENTREALLOCATIONS.LIST>
</CATEGORYALLOCATIONS.LIST>
```

Mapped to:
- `tags.name` = Cost center name
- `transaction_tags.allocated_amount` = Allocation amount
- `transaction_tags.transaction_type` = Entity type
- `transaction_tags.transaction_id` = Entity ID

---

## Deduplication

All imports use `tally_voucher_guid` for deduplication:

```csharp
var existing = await _repository.GetByTallyGuidAsync(companyId, voucher.Guid);
if (existing != null) {
    counts.Skipped++;
    continue;
}
```

## Migration Logging

All imports are logged to `tally_migration_logs`:

| Field | Value |
|-------|-------|
| `batch_id` | Import batch ID |
| `record_type` | `voucher_sales`, `voucher_payment`, etc. |
| `tally_guid` | Voucher GUID |
| `tally_name` | `{VoucherType}/{VoucherNumber}` |
| `tally_date` | Voucher date |
| `tally_amount` | Voucher amount |
| `status` | `success`, `failed`, `skipped` |
| `error_message` | Error details if failed |
| `target_id` | Created entity ID |
| `target_entity` | Target table name |

---

## Bill Allocations (Payment-Invoice Linking)

Bill allocations from Tally are persisted and used to:
1. Create `payment_allocations` records (for customer receipts)
2. Create `vendor_payment_allocations` records (for vendor payments)
3. Auto-update invoice status (`paid`, `partially_paid`) based on total allocations

### Allocation Type Mapping

| Tally Bill Type | Allocation Type | Invoice Linked |
|-----------------|-----------------|----------------|
| `Agst Ref` / `Against Reference` | `invoice_settlement` | Yes |
| `New Ref` | `advance` | No (advance payment) |
| `Advance` | `advance` | No |
| `On Account` | `on_account` | No (unallocated) |

### Invoice Status Updates

After allocations are created, invoice status is automatically updated:
- `paid` - when total allocations >= invoice total
- `partially_paid` - when allocations > 0 but < total
- Original status preserved if no allocations

---

## Known Gaps / Notes

1. **Receipt Typing**: Uses bill allocations only; narration text is not used for `payment_type` determination.

2. **Stock Vouchers**: Currently imported as journal entries. Stock movements not fully implemented (not needed for services companies).

3. **Unmapped Ledgers**: Uses suspense account (`SUSPENSE-IMPORT`). Review these post-import.

4. **Unmapped Vendors**: Uses placeholder vendor (`UNKNOWN-TALLY-VENDOR`). Review post-import.

5. **Place of Supply**: Converted from state names to GST state codes (truncated to 5 chars).

---

## Code References

- `backend/src/Application/Services/Migration/TallyXmlParserService.cs` - XML parsing
- `backend/src/Application/Services/Migration/TallyVoucherMappingService.cs` - Main mapping orchestration
- `backend/src/Application/Services/Migration/TallyPaymentClassifier.cs` - Payment classification
- `backend/src/Application/Services/Migration/TallyContractorPaymentMapper.cs` - Contractor payments
- `backend/src/Application/Services/Migration/TallyStatutoryPaymentMapper.cs` - Statutory payments
- `backend/src/Application/Services/Migration/TallyBankTransactionMapper.cs` - Bank transactions
- `backend/src/Application/DTOs/Migration/TallyVoucherDtos.cs` - DTO definitions
