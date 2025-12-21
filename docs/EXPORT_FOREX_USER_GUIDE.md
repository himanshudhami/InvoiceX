# Export & Forex Management User Guide

A comprehensive guide for using the Export & Forex management module in the Invoice System. This module handles FEMA compliance, FIRC tracking, LUT management, and forex gain/loss accounting for export businesses.

---

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Navigation](#navigation)
4. [Export Dashboard](#export-dashboard)
5. [FIRC Management](#firc-management)
6. [LUT Register](#lut-register)
7. [FEMA Compliance](#fema-compliance)
8. [Receivables Ageing](#receivables-ageing)
9. [API Reference](#api-reference)
10. [Troubleshooting](#troubleshooting)

---

## Overview

This module is designed for **100% export service businesses** operating in India. It helps you:

- **Track FIRCs** (Foreign Inward Remittance Certificates) from banks
- **Manage LUTs** (Letter of Undertaking) for GST zero-rated exports
- **Monitor FEMA Compliance** with the 270-day realization deadline
- **Calculate Forex Gain/Loss** (realized and unrealized)
- **Age Export Receivables** with FEMA deadline tracking

### Key Compliance Requirements Addressed

| Regulation | Requirement | Module Feature |
|------------|-------------|----------------|
| **FEMA** | Realize export proceeds within 270 days | Realization deadline tracking, alerts |
| **RBI/EDPMS** | Report FIRCs to EDPMS portal | FIRC management with EDPMS status |
| **GST** | Valid LUT for zero-rated exports | LUT register with expiry alerts |
| **Ind AS 21** | Foreign currency accounting | Forex gain/loss calculation |

---

## Getting Started

### Prerequisites

1. **Company Setup**: Ensure your company is configured in the system with correct GSTIN and export settings
2. **Invoices**: Create export invoices in foreign currency (USD, EUR, etc.)
3. **Payments**: Record payments received from foreign customers
4. **Bank Accounts**: Set up your Authorized Dealer (AD) bank accounts

### Initial Configuration

1. **Add LUT for Current Financial Year**:
   - Navigate to **Exports & Forex → LUT Register**
   - Click **+ New LUT**
   - Enter LUT number, ARN, validity dates, and GSTIN
   - This ensures all new export invoices are compliant

2. **Set Up Exchange Rates**:
   - Exchange rates are captured at invoice date and payment date
   - The system calculates forex gain/loss automatically

---

## Navigation

Access the Export & Forex module from the left sidebar:

```
📊 Exports & Forex
├── Export Dashboard      (/exports)
├── FIRC Management       (/exports/firc)
├── LUT Register          (/exports/lut)
├── FEMA Compliance       (/exports/fema)
└── Receivables Ageing    (/exports/ageing)
```

---

## Export Dashboard

**Path**: `/exports`

The Export Dashboard is your command center for export operations. It provides:

### KPI Cards

| Metric | Description |
|--------|-------------|
| **Total Export Revenue (YTD)** | Year-to-date export revenue in USD and INR |
| **Outstanding Receivables** | Current unpaid export invoices |
| **Realized Forex Gain/Loss** | Actual gain/loss from settled receivables |
| **Unrealized Forex Position** | MTM gain/loss on open receivables |

### Charts

1. **Realization Trend**: Monthly export realization amounts over 12 months
2. **Currency Distribution**: Pie chart showing receivables by currency
3. **Ageing Distribution**: Bar chart showing receivables by age bucket

### Quick Links

- View at-risk invoices (approaching 270-day deadline)
- Check pending FIRC entries
- Review LUT expiry status

---

## FIRC Management

**Path**: `/exports/firc`

FIRCs (Foreign Inward Remittance Certificates) are issued by banks when foreign currency is credited. They are essential for FEMA compliance.

### Step-by-Step: Recording a FIRC

1. **Navigate** to FIRC Management
2. **Click** "+ New FIRC"
3. **Fill in** the FIRC details:
   - **FIRC Number**: Certificate number from bank
   - **FIRC Date**: Date on certificate
   - **Bank Name**: Your AD bank
   - **Foreign Currency & Amount**: USD 10,000
   - **INR Amount**: ₹8,30,000
   - **Exchange Rate**: 83.00
   - **Remitter Details**: Customer name and country
   - **Purpose Code**: P0802 (Software Services)
4. **Save** the FIRC

### Linking FIRC to Payment

1. Open the FIRC record
2. Click **Link to Payment**
3. Select the matching payment from the list
4. The system auto-suggests matches based on amount and date

### Linking FIRC to Invoices

1. Open the FIRC record
2. Click **Link to Invoices**
3. Select one or more invoices covered by this FIRC
4. Allocate amounts if FIRC covers multiple invoices

### Auto-Match Feature

1. Click **Auto Match** button
2. System matches FIRCs with payments based on:
   - Amount (within tolerance)
   - Date (within 5 days)
3. Review suggestions and confirm matches

### EDPMS Reporting

1. After linking FIRC to invoices, mark it as **EDPMS Reported**
2. Enter the report date and reference number
3. This tracks RBI compliance status

### FIRC Status Flow

```
Received → Linked to Payment → Linked to Invoices → EDPMS Reported
```

---

## LUT Register

**Path**: `/exports/lut`

LUT (Letter of Undertaking) is required for zero-rated GST supplies (exports without paying IGST).

### Understanding LUT

- **Validity**: April 1 to March 31 (Financial Year)
- **Purpose**: Export without paying IGST, claim input credit refund
- **Requirement**: Must have valid LUT before creating export invoices

### Recording a New LUT

1. Navigate to LUT Register
2. Click **+ New LUT**
3. Enter:
   - **LUT Number**: From GST portal
   - **ARN (Application Reference Number)**: GST portal reference
   - **Financial Year**: e.g., "2025-26"
   - **Valid From**: Usually April 1
   - **Valid To**: Usually March 31
   - **GSTIN**: Your company's GSTIN
4. Save

### LUT Renewal

Before the current LUT expires:

1. Open the expiring LUT
2. Click **Renew**
3. Enter new LUT details for next FY
4. Old LUT is marked as "Superseded"

### Expiry Alerts

- System shows alerts 30 days before LUT expiry
- Check **Expiry Alerts** section for upcoming expirations
- Renew before creating invoices in new financial year

### Validation

When creating export invoices, the system:
1. Checks if valid LUT exists for invoice date
2. Warns if no valid LUT found
3. Links LUT details to invoice

---

## FEMA Compliance

**Path**: `/exports/fema`

Monitor compliance with the 270-day export realization requirement.

### Compliance Score

The dashboard shows an overall compliance score:
- **90-100%**: Excellent - All receivables within safe limits
- **70-89%**: Good - Some receivables approaching deadline
- **50-69%**: Warning - Action required on multiple invoices
- **Below 50%**: Critical - FEMA violations possible

### Compliance Checklist

| Item | Status | Description |
|------|--------|-------------|
| LUT Valid | ✅/❌ | Current LUT is active |
| No FEMA Violations | ✅/❌ | No invoices past 270 days |
| FIRCs Linked | ✅/❌ | All payments have FIRC |
| EDPMS Reported | ✅/❌ | All FIRCs reported to EDPMS |

### Violation Alerts

Lists invoices that have violated or are close to violating FEMA:

| Severity | Days Outstanding | Action Required |
|----------|------------------|-----------------|
| **At Risk** | 180-240 days | Follow up with customer |
| **Critical** | 240-270 days | Escalate immediately |
| **Violated** | 270+ days | RBI extension may be needed |

### Realization Status Chart

Pie chart showing distribution:
- **Realized**: Payments received
- **Safe**: Outstanding < 180 days
- **At Risk**: 180-270 days
- **Overdue**: > 270 days (FEMA violation)

---

## Receivables Ageing

**Path**: `/exports/ageing`

Track outstanding export receivables with FEMA deadline awareness.

### Ageing Buckets

| Bucket | Days Outstanding | Color Code |
|--------|------------------|------------|
| Current | 0-30 | 🟢 Green |
| 31-60 | 31-60 | 🟢 Light Green |
| 61-90 | 61-90 | 🟡 Yellow |
| 91-180 | 91-180 | 🟠 Orange |
| 181-270 | 181-270 | 🔴 Red |
| 270+ | Over 270 | ⬛ Dark Red (FEMA Violation) |

### Report Features

1. **Summary Cards**:
   - Total Outstanding (USD)
   - Total Outstanding (INR)
   - Unrealized Forex Gain/Loss
   - Weighted Average Age

2. **Ageing Chart**: Bar chart by bucket

3. **Invoice Details Table**:
   - Invoice Number
   - Customer
   - Amount (USD & INR)
   - Age Badge
   - Days to FEMA Deadline
   - Progress Bar (% of 270 days used)
   - Forex Impact

4. **Customer Summary**: Grouped by customer

### Filtering

- Filter by age bucket
- Search by invoice number or customer
- Filter by company

### Export

Click **Export** to download ageing report as CSV.

---

## API Reference

### Base URL

```
http://localhost:5000/api
```

### FIRC Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/firc/paged` | Get paginated FIRCs |
| GET | `/firc/{id}` | Get FIRC by ID |
| POST | `/firc` | Create new FIRC |
| PUT | `/firc/{id}` | Update FIRC |
| DELETE | `/firc/{id}` | Delete FIRC |
| POST | `/firc/{id}/link-payment/{paymentId}` | Link to payment |
| POST | `/firc/{id}/link-invoices` | Link to invoices |
| POST | `/firc/auto-match/{companyId}` | Auto-match FIRCs |
| POST | `/firc/{id}/mark-edpms-reported` | Mark EDPMS reported |
| GET | `/firc/realization-alerts/{companyId}` | Get FEMA alerts |

### LUT Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/lut/paged` | Get paginated LUTs |
| GET | `/lut/{id}` | Get LUT by ID |
| POST | `/lut` | Create new LUT |
| PUT | `/lut/{id}` | Update LUT |
| DELETE | `/lut/{id}` | Delete LUT |
| GET | `/lut/active/{companyId}/{fy}` | Get active LUT |
| GET | `/lut/validate/{companyId}?date=` | Validate for date |
| POST | `/lut/{id}/renew` | Renew LUT |
| GET | `/lut/expiry-alerts` | Get expiry alerts |

### Export Reporting Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/exportreporting/dashboard/{companyId}` | Export dashboard |
| GET | `/exportreporting/receivables-ageing/{companyId}` | Ageing report |
| GET | `/exportreporting/forex-gain-loss/{companyId}` | Forex report |
| GET | `/exportreporting/fema-dashboard/{companyId}` | FEMA compliance |
| GET | `/exportreporting/realization-trend/{companyId}` | Monthly trend |

---

## Troubleshooting

### Common Issues

#### "No valid LUT found for invoice date"

**Cause**: No active LUT exists for the invoice date
**Solution**: Create or renew LUT in LUT Register before creating export invoices

#### "FIRC amount doesn't match payment"

**Cause**: Currency conversion or bank charges
**Solution**: Use Auto-Match with tolerance settings, or manually link with allocation

#### "Export receivable past 270 days"

**Cause**: FEMA realization deadline exceeded
**Solution**:
1. Follow up with customer urgently
2. If needed, apply for RBI extension
3. Document all follow-up actions

#### "Exchange rate missing on invoice"

**Cause**: Invoice created before forex feature was implemented
**Solution**: Run backfill migration or manually update exchange rates

### Getting Help

1. Check this documentation
2. Review API error messages
3. Contact system administrator
4. For FEMA/RBI compliance questions, consult your CA

---

## Appendix: Key Terms

| Term | Full Form | Description |
|------|-----------|-------------|
| **FEMA** | Foreign Exchange Management Act | Governs forex transactions in India |
| **FIRC** | Foreign Inward Remittance Certificate | Bank certificate for incoming forex |
| **EDPMS** | Export Data Processing & Monitoring System | RBI portal for export tracking |
| **LUT** | Letter of Undertaking | GST exemption for exports |
| **AD Bank** | Authorized Dealer Bank | Bank authorized for forex transactions |
| **MTM** | Mark-to-Market | Revaluation at current rates |
| **Ind AS 21** | Indian Accounting Standard 21 | Foreign currency translation |

---

*Document Version: 1.0*
*Last Updated: December 2025*
