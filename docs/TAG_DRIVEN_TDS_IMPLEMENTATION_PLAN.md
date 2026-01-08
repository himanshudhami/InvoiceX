# Tag-Driven TDS & Classification System - Implementation Plan

## Executive Summary

A modern, tag-driven approach to party classification and TDS management that:
- **Auto-classifies** parties during Tally import based on ledger groups
- **Uses tags to drive TDS behavior** instead of hard-coded vendor types
- **Learns from patterns** in bank narrations and transactions
- **Complies with Indian tax laws** (Income Tax Act sections 194A-195)

Since this is a fresh implementation (no production data), we can build this clean without legacy support.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           TAG-DRIVEN TDS SYSTEM                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                  │
│  │ Tally Import │───▶│ Auto-Tagging │───▶│ TDS Detection│                  │
│  │   Ledger     │    │   Engine     │    │   Service    │                  │
│  │   Groups     │    │              │    │              │                  │
│  └──────────────┘    └──────────────┘    └──────────────┘                  │
│         │                   │                   │                          │
│         ▼                   ▼                   ▼                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        TAGS (Master Data)                           │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │  TDS:194C-Contractor  │  TDS:194J-Professional  │  etc...   │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │  MSME:Registered  │  GST:Composition  │  Govt:Entity  │     │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│         │                                                                   │
│         ▼                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     TDS_TAG_RULES (Behavior)                        │   │
│  │  Tag: TDS:194C-Contractor → Rate: 2%, Threshold: ₹30,000/yr        │   │
│  │  Tag: TDS:194J-Professional → Rate: 10%, Threshold: ₹50,000/yr     │   │
│  │  Tag: Govt:Entity → TDS Exempt                                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 1: Seed Default TDS Tags & Rules

### 1.1 Create TDS Classification Tags

These tags represent TDS sections under the Income Tax Act, 1961:

```sql
-- TDS Section Tags (tag_group: 'tds_section')
INSERT INTO tags (company_id, name, tag_group, color, description, full_path, level) VALUES
-- Section 194C: Contractor Payments
('{{company_id}}', 'TDS:194C-Contractor', 'tds_section', '#FF6B6B',
 'Contractors - TDS u/s 194C @ 1%/2%', '/TDS:194C-Contractor', 0),

-- Section 194J: Professional/Technical Services
('{{company_id}}', 'TDS:194J-Professional', 'tds_section', '#4ECDC4',
 'Professional fees - TDS u/s 194J @ 10%', '/TDS:194J-Professional', 0),

-- Section 194J Technical (Separate rate for technical services)
('{{company_id}}', 'TDS:194J-Technical', 'tds_section', '#45B7D1',
 'Technical services - TDS u/s 194J @ 2%', '/TDS:194J-Technical', 0),

-- Section 194H: Commission/Brokerage
('{{company_id}}', 'TDS:194H-Commission', 'tds_section', '#96CEB4',
 'Commission/Brokerage - TDS u/s 194H @ 5%', '/TDS:194H-Commission', 0),

-- Section 194I: Rent
('{{company_id}}', 'TDS:194I-Rent-Land', 'tds_section', '#FFEAA7',
 'Rent for land/building - TDS u/s 194I @ 10%', '/TDS:194I-Rent-Land', 0),

('{{company_id}}', 'TDS:194I-Rent-Equipment', 'tds_section', '#DDA0DD',
 'Rent for plant/machinery/equipment - TDS u/s 194I @ 2%', '/TDS:194I-Rent-Equipment', 0),

-- Section 194A: Interest (other than securities)
('{{company_id}}', 'TDS:194A-Interest', 'tds_section', '#98D8C8',
 'Interest payments - TDS u/s 194A @ 10%', '/TDS:194A-Interest', 0),

-- Section 194Q: Purchase of Goods (for large buyers)
('{{company_id}}', 'TDS:194Q-Purchase', 'tds_section', '#F7DC6F',
 'Purchase of goods - TDS u/s 194Q @ 0.1%', '/TDS:194Q-Purchase', 0),

-- Section 195: Foreign Payments
('{{company_id}}', 'TDS:195-Foreign', 'tds_section', '#BB8FCE',
 'Foreign payments - TDS u/s 195 @ DTAA rates', '/TDS:195-Foreign', 0),

-- Section 194M: Contractual work to individuals/HUF
('{{company_id}}', 'TDS:194M-Individual', 'tds_section', '#F8B500',
 'Payments to individuals/HUF - TDS u/s 194M @ 5%', '/TDS:194M-Individual', 0),

-- TDS Exempt
('{{company_id}}', 'TDS:Exempt', 'tds_section', '#27AE60',
 'TDS not applicable', '/TDS:Exempt', 0);
```

### 1.2 Create TDS Tag Rules Table

New table to link tags to TDS behavior:

```sql
-- Migration: xxx_tds_tag_rules.sql
CREATE TABLE tds_tag_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    tag_id UUID NOT NULL REFERENCES tags(id),

    -- TDS Section Info
    tds_section VARCHAR(10) NOT NULL,  -- '194C', '194J', etc.
    tds_section_clause VARCHAR(20),     -- '194J(a)', '194J(ba)' for sub-clauses

    -- Rate Configuration (FY 2024-25 rates)
    tds_rate_with_pan DECIMAL(5,2) NOT NULL,      -- Rate when PAN available
    tds_rate_without_pan DECIMAL(5,2) NOT NULL,   -- Rate when PAN not available (usually 20%)
    tds_rate_individual DECIMAL(5,2),             -- Different rate for individuals (194C: 1%)
    tds_rate_company DECIMAL(5,2),                -- Different rate for companies (194C: 2%)

    -- Thresholds (per Income Tax Act)
    threshold_single_payment DECIMAL(18,2),       -- Per payment threshold
    threshold_annual DECIMAL(18,2) NOT NULL,      -- Annual threshold

    -- Applicability
    applies_to_individual BOOLEAN DEFAULT true,
    applies_to_huf BOOLEAN DEFAULT true,
    applies_to_company BOOLEAN DEFAULT true,
    applies_to_firm BOOLEAN DEFAULT true,
    applies_to_llp BOOLEAN DEFAULT true,
    applies_to_trust BOOLEAN DEFAULT true,
    applies_to_aop_boi BOOLEAN DEFAULT true,
    applies_to_government BOOLEAN DEFAULT false,

    -- Special Rules
    lower_certificate_allowed BOOLEAN DEFAULT true,
    nil_certificate_allowed BOOLEAN DEFAULT true,

    -- Exemptions
    exemption_notes TEXT,

    -- Metadata
    effective_from DATE NOT NULL DEFAULT '2024-04-01',
    effective_to DATE,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),

    UNIQUE(company_id, tag_id, effective_from)
);

CREATE INDEX idx_tds_tag_rules_company ON tds_tag_rules(company_id);
CREATE INDEX idx_tds_tag_rules_tag ON tds_tag_rules(tag_id);
CREATE INDEX idx_tds_tag_rules_section ON tds_tag_rules(tds_section);
```

### 1.3 Seed TDS Rules (FY 2024-25 Rates)

```sql
-- Seed TDS rules based on tags (run after tags are created)
INSERT INTO tds_tag_rules (
    company_id, tag_id, tds_section,
    tds_rate_with_pan, tds_rate_without_pan,
    tds_rate_individual, tds_rate_company,
    threshold_single_payment, threshold_annual,
    exemption_notes
) VALUES
-- 194C: Contractors
((SELECT id FROM tags WHERE name = 'TDS:194C-Contractor' AND company_id = '{{company_id}}'),
 '194C', 1.00, 20.00, 1.00, 2.00, 30000.00, 100000.00,
 'Exempt: Payments to transporters owning ≤10 goods carriages who furnish PAN'),

-- 194J: Professional Services
((SELECT id FROM tags WHERE name = 'TDS:194J-Professional' AND company_id = '{{company_id}}'),
 '194J', 10.00, 20.00, 10.00, 10.00, NULL, 50000.00,
 'Includes legal, medical, engineering, architectural, accountancy, technical consultancy, interior decoration, advertising'),

-- 194J: Technical Services (separate 2% rate per Budget 2020)
((SELECT id FROM tags WHERE name = 'TDS:194J-Technical' AND company_id = '{{company_id}}'),
 '194J', 2.00, 20.00, 2.00, 2.00, NULL, 50000.00,
 'Technical services, royalty for sale/distribution of cinematographic films'),

-- 194H: Commission/Brokerage
((SELECT id FROM tags WHERE name = 'TDS:194H-Commission' AND company_id = '{{company_id}}'),
 '194H', 5.00, 20.00, 5.00, 5.00, NULL, 15000.00,
 'Commission or brokerage excluding insurance commission'),

-- 194I: Rent (Land/Building)
((SELECT id FROM tags WHERE name = 'TDS:194I-Rent-Land' AND company_id = '{{company_id}}'),
 '194I(a)', 10.00, 20.00, 10.00, 10.00, NULL, 240000.00,
 'Rent of land, building, land appurtenant to building, furniture, fittings'),

-- 194I: Rent (Plant/Machinery)
((SELECT id FROM tags WHERE name = 'TDS:194I-Rent-Equipment' AND company_id = '{{company_id}}'),
 '194I(b)', 2.00, 20.00, 2.00, 2.00, NULL, 240000.00,
 'Rent of plant, machinery, equipment'),

-- 194A: Interest
((SELECT id FROM tags WHERE name = 'TDS:194A-Interest' AND company_id = '{{company_id}}'),
 '194A', 10.00, 20.00, 10.00, 10.00, NULL, 50000.00,
 'Interest other than interest on securities. Senior citizens: ₹50,000 threshold'),

-- 194Q: Purchase of Goods
((SELECT id FROM tags WHERE name = 'TDS:194Q-Purchase' AND company_id = '{{company_id}}'),
 '194Q', 0.10, 5.00, 0.10, 0.10, NULL, 5000000.00,
 'Only for buyers with turnover > ₹10 Cr. Does not apply if seller is liable for TCS u/s 206C(1H)'),

-- 195: Foreign Payments
((SELECT id FROM tags WHERE name = 'TDS:195-Foreign' AND company_id = '{{company_id}}'),
 '195', 10.00, 20.00, 10.00, 10.00, NULL, 0.00,
 'Rate varies by DTAA. Default 10% for royalties/FTS. Get CA certificate for DTAA rates'),

-- 194M: Payments to Individuals/HUF
((SELECT id FROM tags WHERE name = 'TDS:194M-Individual' AND company_id = '{{company_id}}'),
 '194M', 5.00, 20.00, 5.00, 5.00, NULL, 5000000.00,
 'For individuals/HUF not liable for audit. Threshold: ₹50 lakh aggregate in FY'),

-- TDS Exempt (Government entities, etc.)
((SELECT id FROM tags WHERE name = 'TDS:Exempt' AND company_id = '{{company_id}}'),
 'EXEMPT', 0.00, 0.00, 0.00, 0.00, NULL, NULL,
 'Government entities, specified exempted parties');
```

---

## Phase 2: Party Classification Tags

### 2.1 Create Classification Tags

```sql
-- Party Type Tags (tag_group: 'party_type')
INSERT INTO tags (company_id, name, tag_group, color, description, full_path) VALUES
-- Vendor Classifications
('{{company_id}}', 'Vendor:Supplier', 'party_type', '#3498DB', 'Regular goods supplier', '/Vendor:Supplier'),
('{{company_id}}', 'Vendor:Contractor', 'party_type', '#E74C3C', 'Works contractor', '/Vendor:Contractor'),
('{{company_id}}', 'Vendor:Consultant', 'party_type', '#9B59B6', 'Professional consultant', '/Vendor:Consultant'),
('{{company_id}}', 'Vendor:Landlord', 'party_type', '#F39C12', 'Property/asset lessor', '/Vendor:Landlord'),
('{{company_id}}', 'Vendor:Service', 'party_type', '#1ABC9C', 'Service provider', '/Vendor:Service'),
('{{company_id}}', 'Vendor:Foreign', 'party_type', '#34495E', 'Non-resident vendor', '/Vendor:Foreign'),

-- Customer Classifications
('{{company_id}}', 'Customer:B2B', 'party_type', '#2ECC71', 'Business customer', '/Customer:B2B'),
('{{company_id}}', 'Customer:B2C', 'party_type', '#27AE60', 'Consumer customer', '/Customer:B2C'),
('{{company_id}}', 'Customer:Export', 'party_type', '#16A085', 'Export customer', '/Customer:Export'),
('{{company_id}}', 'Customer:SEZ', 'party_type', '#1E8449', 'SEZ customer', '/Customer:SEZ'),
('{{company_id}}', 'Customer:Government', 'party_type', '#145A32', 'Government entity', '/Customer:Government'),

-- Compliance Tags
('{{company_id}}', 'MSME:Micro', 'compliance', '#FF9F43', 'MSME Micro enterprise', '/MSME:Micro'),
('{{company_id}}', 'MSME:Small', 'compliance', '#FECA57', 'MSME Small enterprise', '/MSME:Small'),
('{{company_id}}', 'MSME:Medium', 'compliance', '#EE5A24', 'MSME Medium enterprise', '/MSME:Medium'),
('{{company_id}}', 'GST:Composition', 'compliance', '#0ABDE3', 'GST Composition dealer', '/GST:Composition'),
('{{company_id}}', 'GST:Exempt', 'compliance', '#10AC84', 'GST exempt supplier', '/GST:Exempt'),
('{{company_id}}', 'PAN:Verified', 'compliance', '#2ECC71', 'PAN verified', '/PAN:Verified'),
('{{company_id}}', 'PAN:Invalid', 'compliance', '#E74C3C', 'PAN invalid/not provided', '/PAN:Invalid');
```

### 2.2 Tally Group to Tag Mapping Rules

```sql
-- Tally Ledger Group → Tag Mapping (stored in tally_field_mappings with target_entity = 'tag')
INSERT INTO tally_field_mappings (
    company_id, mapping_type, tally_group_name, target_entity,
    target_tag_group, metadata, priority, is_system_default
) VALUES
-- Vendor Groups → TDS + Party Tags
('{{company_id}}', 'ledger_group', 'CONSULTANTS', 'party_tag', 'tds_section',
 '{"tags": ["TDS:194J-Professional", "Vendor:Consultant"]}', 10, true),

('{{company_id}}', 'ledger_group', 'CONTRACTORS', 'party_tag', 'tds_section',
 '{"tags": ["TDS:194C-Contractor", "Vendor:Contractor"]}', 10, true),

('{{company_id}}', 'ledger_group', 'PROFESSIONAL FEES', 'party_tag', 'tds_section',
 '{"tags": ["TDS:194J-Professional", "Vendor:Consultant"]}', 10, true),

('{{company_id}}', 'ledger_group', 'RENT PAYABLE', 'party_tag', 'tds_section',
 '{"tags": ["TDS:194I-Rent-Land", "Vendor:Landlord"]}', 10, true),

('{{company_id}}', 'ledger_group', 'RENT - MACHINERY', 'party_tag', 'tds_section',
 '{"tags": ["TDS:194I-Rent-Equipment", "Vendor:Landlord"]}', 10, true),

('{{company_id}}', 'ledger_group', 'COMMISSION PAYABLE', 'party_tag', 'tds_section',
 '{"tags": ["TDS:194H-Commission", "Vendor:Service"]}', 10, true),

('{{company_id}}', 'ledger_group', 'INTEREST PAYABLE', 'party_tag', 'tds_section',
 '{"tags": ["TDS:194A-Interest"]}', 10, true),

('{{company_id}}', 'ledger_group', 'FOREIGN PAYMENTS', 'party_tag', 'tds_section',
 '{"tags": ["TDS:195-Foreign", "Vendor:Foreign"]}', 10, true),

-- Standard Vendor Groups (no TDS by default - regular suppliers)
('{{company_id}}', 'ledger_group', 'SUNDRY CREDITORS', 'party_tag', 'party_type',
 '{"tags": ["Vendor:Supplier"]}', 10, true),

-- Customer Groups
('{{company_id}}', 'ledger_group', 'SUNDRY DEBTORS', 'party_tag', 'party_type',
 '{"tags": ["Customer:B2B"]}', 10, true),

('{{company_id}}', 'ledger_group', 'EXPORT DEBTORS', 'party_tag', 'party_type',
 '{"tags": ["Customer:Export"]}', 10, true),

('{{company_id}}', 'ledger_group', 'GOVERNMENT DEBTORS', 'party_tag', 'party_type',
 '{"tags": ["Customer:Government", "TDS:Exempt"]}', 10, true);
```

---

## Phase 3: Enhanced Auto-Tagging Engine

### 3.1 Attribution Rules for Pattern Detection

Leverage the existing `attribution_rules` table:

```sql
-- Auto-detection rules based on name patterns
INSERT INTO attribution_rules (
    company_id, name, rule_type, priority, conditions, tag_assignments,
    allocation_method, is_active, stop_on_match
) VALUES
-- Detect consultants by name pattern
('{{company_id}}', 'Consultant Name Pattern', 'vendor', 100,
 '{"any": [
    {"field": "name", "operator": "contains", "value": "CONSULTANT"},
    {"field": "name", "operator": "contains", "value": "CONSULTANCY"},
    {"field": "name", "operator": "contains", "value": "ADVISORY"}
  ]}',
 '["TDS:194J-Professional", "Vendor:Consultant"]',
 'single', true, false),

-- Detect contractors
('{{company_id}}', 'Contractor Name Pattern', 'vendor', 100,
 '{"any": [
    {"field": "name", "operator": "contains", "value": "CONTRACTOR"},
    {"field": "name", "operator": "contains", "value": "CONSTRUCTION"},
    {"field": "name", "operator": "contains", "value": "BUILDERS"},
    {"field": "name", "operator": "contains", "value": "INFRASTRUCTURE"}
  ]}',
 '["TDS:194C-Contractor", "Vendor:Contractor"]',
 'single', true, false),

-- Detect legal services
('{{company_id}}', 'Legal Services Pattern', 'vendor', 100,
 '{"any": [
    {"field": "name", "operator": "contains", "value": "ADVOCATE"},
    {"field": "name", "operator": "contains", "value": "LEGAL"},
    {"field": "name", "operator": "contains", "value": "LAW FIRM"},
    {"field": "name", "operator": "matches", "value": ".*& ASSOCIATES$"}
  ]}',
 '["TDS:194J-Professional", "Vendor:Consultant"]',
 'single', true, false),

-- Detect CA/Accounting firms
('{{company_id}}', 'CA Firm Pattern', 'vendor', 100,
 '{"any": [
    {"field": "name", "operator": "matches", "value": "^.*CA\\s+.*"},
    {"field": "name", "operator": "contains", "value": "CHARTERED ACCOUNTANT"},
    {"field": "name", "operator": "contains", "value": "& CO"}
  ]}',
 '["TDS:194J-Professional", "Vendor:Consultant"]',
 'single', true, false),

-- Detect rent payments (from narration)
('{{company_id}}', 'Rent Payment Pattern', 'keyword', 90,
 '{"any": [
    {"field": "narration", "operator": "contains", "value": "RENT"},
    {"field": "narration", "operator": "contains", "value": "LEASE"},
    {"field": "description", "operator": "contains", "value": "OFFICE RENT"}
  ]}',
 '["TDS:194I-Rent-Land"]',
 'single', true, false),

-- Detect government entities (TDS exempt)
('{{company_id}}', 'Government Entity Pattern', 'vendor', 200,
 '{"any": [
    {"field": "name", "operator": "starts_with", "value": "GOVERNMENT"},
    {"field": "name", "operator": "starts_with", "value": "DEPT OF"},
    {"field": "name", "operator": "starts_with", "value": "MINISTRY OF"},
    {"field": "name", "operator": "contains", "value": "MUNICIPAL"},
    {"field": "name", "operator": "contains", "value": "CORPORATION OF"},
    {"field": "gstin", "operator": "starts_with", "value": "0"}
  ]}',
 '["TDS:Exempt", "Customer:Government"]',
 'single', true, true),

-- Detect foreign vendors (by country code in PAN area or no PAN)
('{{company_id}}', 'Foreign Vendor Pattern', 'vendor', 150,
 '{"all": [
    {"field": "is_vendor", "operator": "equals", "value": true},
    {"any": [
      {"field": "country", "operator": "not_equals", "value": "India"},
      {"field": "country", "operator": "not_equals", "value": "IN"},
      {"field": "pan", "operator": "is_empty", "value": true}
    ]}
  ]}',
 '["TDS:195-Foreign", "Vendor:Foreign"]',
 'single', true, false);
```

### 3.2 Bank Narration Pattern Rules

```sql
-- Bank transaction auto-categorization (like Zoho Books)
INSERT INTO attribution_rules (
    company_id, name, rule_type, priority, conditions, tag_assignments,
    allocation_method, is_active
) VALUES
-- Salary payments
('{{company_id}}', 'Salary Payment', 'keyword', 80,
 '{"any": [
    {"field": "narration", "operator": "contains", "value": "SALARY"},
    {"field": "narration", "operator": "contains", "value": "PAYROLL"},
    {"field": "narration", "operator": "matches", "value": ".*NEFT.*SAL.*"}
  ]}',
 '["Expense:Salary"]', 'single', true),

-- GST Payments
('{{company_id}}', 'GST Payment', 'keyword', 90,
 '{"any": [
    {"field": "narration", "operator": "contains", "value": "CBIC"},
    {"field": "narration", "operator": "contains", "value": "GST PAYMENT"},
    {"field": "narration", "operator": "matches", "value": ".*GSTN.*"}
  ]}',
 '["Expense:GST-Payment"]', 'single', true),

-- TDS Payments
('{{company_id}}', 'TDS Challan Payment', 'keyword', 90,
 '{"any": [
    {"field": "narration", "operator": "contains", "value": "TDS"},
    {"field": "narration", "operator": "contains", "value": "TAX DEDUCTED"},
    {"field": "narration", "operator": "matches", "value": ".*OLTAS.*"}
  ]}',
 '["Expense:TDS-Payment"]', 'single', true),

-- Payment Gateway
('{{company_id}}', 'Payment Gateway', 'keyword', 80,
 '{"any": [
    {"field": "narration", "operator": "contains", "value": "RAZORPAY"},
    {"field": "narration", "operator": "contains", "value": "PAYTM"},
    {"field": "narration", "operator": "contains", "value": "PHONEPE"},
    {"field": "narration", "operator": "contains", "value": "STRIPE"}
  ]}',
 '["Revenue:Payment-Gateway"]', 'single', true);
```

---

## Phase 4: TDS Detection Service Upgrade

### 4.1 New Tag-Driven TDS Detection Logic

Replace the current hard-coded TDS detection with tag-based lookup:

```csharp
// TdsDetectionService.cs (new service)
public class TdsDetectionService : ITdsDetectionService
{
    private readonly ITdsTagRuleRepository _tdsTagRuleRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IAttributionRuleRepository _attributionRuleRepository;

    public async Task<TdsDetectionResult> DetectTdsAsync(
        Guid companyId,
        Guid partyId,
        decimal paymentAmount,
        string? narration = null)
    {
        // 1. Get party with tags
        var party = await _partyRepository.GetByIdWithTagsAsync(partyId);
        if (party == null) return TdsDetectionResult.NotApplicable();

        // 2. Check for TDS exempt tag first
        if (party.Tags.Any(t => t.Name == "TDS:Exempt"))
        {
            return TdsDetectionResult.Exempt("Party tagged as TDS exempt");
        }

        // 3. Check for lower/nil TDS certificate
        if (party.VendorProfile?.LowerTdsCertificateNumber != null &&
            party.VendorProfile?.LowerTdsCertificateValidTo > DateTime.Today)
        {
            return await GetLowerCertificateRateAsync(party);
        }

        // 4. Find TDS section tag on party
        var tdsTag = party.Tags
            .FirstOrDefault(t => t.TagGroup == "tds_section" && t.Name.StartsWith("TDS:"));

        if (tdsTag == null)
        {
            // 5. Try auto-detection from attribution rules
            tdsTag = await TryAutoDetectTdsTagAsync(party, narration);
        }

        if (tdsTag == null)
        {
            // 6. Fallback: Check if Tally group name suggests TDS
            tdsTag = await TryDetectFromTallyGroupAsync(party);
        }

        if (tdsTag == null)
        {
            return TdsDetectionResult.NotApplicable("No TDS tag found");
        }

        // 7. Get TDS rule for this tag
        var tdsRule = await _tdsTagRuleRepository.GetByTagIdAsync(tdsTag.Id);
        if (tdsRule == null)
        {
            return TdsDetectionResult.NotApplicable($"No TDS rule configured for tag {tdsTag.Name}");
        }

        // 8. Check thresholds
        var ytdPayments = await GetYtdPaymentsAsync(companyId, partyId);
        if (ytdPayments + paymentAmount < tdsRule.ThresholdAnnual)
        {
            return TdsDetectionResult.BelowThreshold(
                tdsRule.TdsSection,
                tdsRule.ThresholdAnnual,
                ytdPayments);
        }

        // 9. Calculate TDS rate based on PAN availability and party type
        var tdsRate = CalculateTdsRate(party, tdsRule);

        return new TdsDetectionResult
        {
            IsApplicable = true,
            TdsSection = tdsRule.TdsSection,
            TdsSectionClause = tdsRule.TdsSectionClause,
            TdsRate = tdsRate,
            MatchedTag = tdsTag,
            MatchMethod = "tag",
            ThresholdAnnual = tdsRule.ThresholdAnnual,
            YtdPayments = ytdPayments
        };
    }

    private decimal CalculateTdsRate(Party party, TdsTagRule rule)
    {
        // No PAN = 20% flat rate
        if (string.IsNullOrEmpty(party.Pan) || !IsPanValid(party.Pan))
        {
            return rule.TdsRateWithoutPan;
        }

        // Determine entity type from PAN 4th character
        var entityType = GetEntityTypeFromPan(party.Pan);

        return entityType switch
        {
            "C" => rule.TdsRateCompany ?? rule.TdsRateWithPan,  // Company
            "P" => rule.TdsRateIndividual ?? rule.TdsRateWithPan,  // Individual
            "H" => rule.TdsRateIndividual ?? rule.TdsRateWithPan,  // HUF
            "F" => rule.TdsRateCompany ?? rule.TdsRateWithPan,  // Firm
            "T" => rule.TdsRateCompany ?? rule.TdsRateWithPan,  // Trust
            _ => rule.TdsRateWithPan
        };
    }
}
```

### 4.2 TDS Detection Result DTO

```csharp
public class TdsDetectionResult
{
    public bool IsApplicable { get; set; }
    public string? TdsSection { get; set; }
    public string? TdsSectionClause { get; set; }
    public decimal TdsRate { get; set; }
    public Tag? MatchedTag { get; set; }
    public string MatchMethod { get; set; } = "none"; // "tag", "rule", "tally_group", "manual"
    public decimal? ThresholdAnnual { get; set; }
    public decimal? ThresholdSingle { get; set; }
    public decimal YtdPayments { get; set; }
    public string? ExemptionReason { get; set; }
    public bool IsBelowThreshold { get; set; }
    public string? Notes { get; set; }

    public static TdsDetectionResult NotApplicable(string? reason = null) =>
        new() { IsApplicable = false, Notes = reason };

    public static TdsDetectionResult Exempt(string reason) =>
        new() { IsApplicable = false, ExemptionReason = reason };

    public static TdsDetectionResult BelowThreshold(string section, decimal threshold, decimal ytd) =>
        new() {
            IsApplicable = false,
            TdsSection = section,
            IsBelowThreshold = true,
            ThresholdAnnual = threshold,
            YtdPayments = ytd,
            Notes = $"Below annual threshold of ₹{threshold:N0}. YTD: ₹{ytd:N0}"
        };
}
```

---

## Phase 5: Tally Import Integration

### 5.1 Update TallyMasterMappingService

Modify the vendor/customer import to apply tags based on mappings:

```csharp
// In TallyMasterMappingService.cs

private async Task<Party> CreateVendorFromLedgerAsync(
    TallyLedgerDto ledger,
    Guid companyId,
    Guid batchId)
{
    var party = new Party
    {
        CompanyId = companyId,
        Name = ledger.Name,
        IsVendor = true,
        Pan = ledger.PanItNo,
        Gstin = ledger.GstRegistrationNumber,
        TallyLedgerGuid = ledger.Guid,
        TallyLedgerName = ledger.Name,
        TallyGroupName = ledger.Parent,  // Store for fallback detection
        TallyMigrationBatchId = batchId
    };

    // Create vendor profile
    party.VendorProfile = new PartyVendorProfile
    {
        TdsApplicable = true,  // Default, will be refined by tags
    };

    // AUTO-TAGGING: Apply tags based on Tally group
    var tags = await GetTagsForTallyGroupAsync(companyId, ledger.Parent);
    foreach (var tag in tags)
    {
        party.Tags.Add(new PartyTag
        {
            TagId = tag.Id,
            Source = "migration",
            AssignedAt = DateTime.UtcNow
        });
    }

    // AUTO-TAGGING: Apply name-pattern based tags
    var patternTags = await _attributionRuleService
        .GetMatchingTagsForPartyAsync(companyId, party);
    foreach (var tag in patternTags)
    {
        if (!party.Tags.Any(t => t.TagId == tag.Id))
        {
            party.Tags.Add(new PartyTag
            {
                TagId = tag.Id,
                Source = "rule",
                AssignedAt = DateTime.UtcNow
            });
        }
    }

    // Detect TDS based on applied tags
    var tdsDetection = await _tdsDetectionService
        .DetectTdsFromTagsAsync(companyId, party.Tags.Select(t => t.TagId));

    if (tdsDetection.IsApplicable)
    {
        party.VendorProfile.DefaultTdsSection = tdsDetection.TdsSection;
        party.VendorProfile.DefaultTdsRate = tdsDetection.TdsRate;
    }
    else
    {
        party.VendorProfile.TdsApplicable = false;
    }

    await _partyRepository.AddAsync(party);
    return party;
}

private async Task<List<Tag>> GetTagsForTallyGroupAsync(Guid companyId, string tallyGroupName)
{
    // Lookup mapping from tally_field_mappings
    var mapping = await _tallyFieldMappingRepository
        .GetByGroupNameAsync(companyId, tallyGroupName, "party_tag");

    if (mapping?.Metadata == null) return new List<Tag>();

    var tagNames = JsonSerializer.Deserialize<string[]>(
        mapping.Metadata.GetProperty("tags").GetRawText());

    var tags = new List<Tag>();
    foreach (var tagName in tagNames)
    {
        var tag = await _tagRepository.GetByNameAsync(companyId, tagName);
        if (tag != null) tags.Add(tag);
    }

    return tags;
}
```

### 5.2 Tally Mapping Configuration UI

Create the missing mapping configuration component:

```typescript
// TallyMappingConfig.tsx
interface TallyGroupMapping {
  tallyGroupName: string;
  targetEntity: 'vendors' | 'customers' | 'bank_accounts' | 'chart_of_accounts';
  tags: string[];  // Tag names to auto-apply
  suggestedTdsSection?: string;
}

const TallyMappingConfig: React.FC<Props> = ({ parsedData, onConfigured }) => {
  const [mappings, setMappings] = useState<TallyGroupMapping[]>([]);
  const { data: tags } = useTags();
  const tdsTagOptions = tags?.filter(t => t.tagGroup === 'tds_section') ?? [];

  // Pre-populate with detected groups
  useEffect(() => {
    const groups = parsedData.ledgerGroups.map(group => ({
      tallyGroupName: group.name,
      targetEntity: detectTargetEntity(group.name),
      tags: detectDefaultTags(group.name),
      suggestedTdsSection: detectTdsSection(group.name)
    }));
    setMappings(groups);
  }, [parsedData]);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Configure Ledger Group Mappings</CardTitle>
        <CardDescription>
          Map Tally ledger groups to entities and select applicable TDS tags
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Tally Group</TableHead>
              <TableHead>Import As</TableHead>
              <TableHead>TDS Section</TableHead>
              <TableHead>Additional Tags</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {mappings.map((mapping, idx) => (
              <TableRow key={mapping.tallyGroupName}>
                <TableCell className="font-medium">
                  {mapping.tallyGroupName}
                </TableCell>
                <TableCell>
                  <Select
                    value={mapping.targetEntity}
                    onValueChange={(v) => updateMapping(idx, 'targetEntity', v)}
                  >
                    <SelectItem value="vendors">Vendors</SelectItem>
                    <SelectItem value="customers">Customers</SelectItem>
                    <SelectItem value="bank_accounts">Bank Accounts</SelectItem>
                    <SelectItem value="chart_of_accounts">GL Accounts</SelectItem>
                    <SelectItem value="skip">Skip Import</SelectItem>
                  </Select>
                </TableCell>
                <TableCell>
                  <Select
                    value={mapping.suggestedTdsSection}
                    onValueChange={(v) => updateMapping(idx, 'suggestedTdsSection', v)}
                    disabled={mapping.targetEntity !== 'vendors'}
                  >
                    <SelectItem value="">No TDS</SelectItem>
                    {tdsTagOptions.map(tag => (
                      <SelectItem key={tag.id} value={tag.name}>
                        {tag.name.replace('TDS:', '')}
                      </SelectItem>
                    ))}
                  </Select>
                </TableCell>
                <TableCell>
                  <MultiSelect
                    options={tags}
                    value={mapping.tags}
                    onChange={(v) => updateMapping(idx, 'tags', v)}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
      <CardFooter>
        <Button onClick={() => onConfigured(mappings)}>
          Continue to Import
        </Button>
      </CardFooter>
    </Card>
  );
};
```

---

## Phase 6: Database Migration Script

### 6.1 Complete Migration

```sql
-- Migration: 140_tag_driven_tds_system.sql

BEGIN;

-- 1. Create TDS tag rules table
CREATE TABLE IF NOT EXISTS tds_tag_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,

    tds_section VARCHAR(10) NOT NULL,
    tds_section_clause VARCHAR(20),

    tds_rate_with_pan DECIMAL(5,2) NOT NULL,
    tds_rate_without_pan DECIMAL(5,2) NOT NULL DEFAULT 20.00,
    tds_rate_individual DECIMAL(5,2),
    tds_rate_company DECIMAL(5,2),

    threshold_single_payment DECIMAL(18,2),
    threshold_annual DECIMAL(18,2) NOT NULL,

    applies_to_individual BOOLEAN DEFAULT true,
    applies_to_huf BOOLEAN DEFAULT true,
    applies_to_company BOOLEAN DEFAULT true,
    applies_to_firm BOOLEAN DEFAULT true,
    applies_to_llp BOOLEAN DEFAULT true,
    applies_to_trust BOOLEAN DEFAULT true,
    applies_to_aop_boi BOOLEAN DEFAULT true,
    applies_to_government BOOLEAN DEFAULT false,

    lower_certificate_allowed BOOLEAN DEFAULT true,
    nil_certificate_allowed BOOLEAN DEFAULT true,
    exemption_notes TEXT,

    effective_from DATE NOT NULL DEFAULT '2024-04-01',
    effective_to DATE,
    is_active BOOLEAN DEFAULT true,

    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),

    UNIQUE(company_id, tag_id, effective_from)
);

CREATE INDEX idx_tds_tag_rules_company ON tds_tag_rules(company_id);
CREATE INDEX idx_tds_tag_rules_tag ON tds_tag_rules(tag_id);
CREATE INDEX idx_tds_tag_rules_section ON tds_tag_rules(tds_section);
CREATE INDEX idx_tds_tag_rules_active ON tds_tag_rules(company_id, is_active) WHERE is_active = true;

-- 2. Function to seed default TDS tags for a company
CREATE OR REPLACE FUNCTION seed_tds_tags(p_company_id UUID)
RETURNS void AS $$
DECLARE
    v_tag_id UUID;
BEGIN
    -- TDS Section Tags
    INSERT INTO tags (company_id, name, tag_group, color, description, full_path, level, is_system)
    VALUES
        (p_company_id, 'TDS:194C-Contractor', 'tds_section', '#FF6B6B',
         'Contractors - TDS u/s 194C @ 1%/2%', '/TDS:194C-Contractor', 0, true),
        (p_company_id, 'TDS:194J-Professional', 'tds_section', '#4ECDC4',
         'Professional fees - TDS u/s 194J @ 10%', '/TDS:194J-Professional', 0, true),
        (p_company_id, 'TDS:194J-Technical', 'tds_section', '#45B7D1',
         'Technical services - TDS u/s 194J @ 2%', '/TDS:194J-Technical', 0, true),
        (p_company_id, 'TDS:194H-Commission', 'tds_section', '#96CEB4',
         'Commission/Brokerage - TDS u/s 194H @ 5%', '/TDS:194H-Commission', 0, true),
        (p_company_id, 'TDS:194I-Rent-Land', 'tds_section', '#FFEAA7',
         'Rent for land/building - TDS u/s 194I @ 10%', '/TDS:194I-Rent-Land', 0, true),
        (p_company_id, 'TDS:194I-Rent-Equipment', 'tds_section', '#DDA0DD',
         'Rent for plant/machinery - TDS u/s 194I @ 2%', '/TDS:194I-Rent-Equipment', 0, true),
        (p_company_id, 'TDS:194A-Interest', 'tds_section', '#98D8C8',
         'Interest payments - TDS u/s 194A @ 10%', '/TDS:194A-Interest', 0, true),
        (p_company_id, 'TDS:194Q-Purchase', 'tds_section', '#F7DC6F',
         'Purchase of goods - TDS u/s 194Q @ 0.1%', '/TDS:194Q-Purchase', 0, true),
        (p_company_id, 'TDS:195-Foreign', 'tds_section', '#BB8FCE',
         'Foreign payments - TDS u/s 195', '/TDS:195-Foreign', 0, true),
        (p_company_id, 'TDS:194M-Individual', 'tds_section', '#F8B500',
         'Payments to individuals/HUF - TDS u/s 194M @ 5%', '/TDS:194M-Individual', 0, true),
        (p_company_id, 'TDS:Exempt', 'tds_section', '#27AE60',
         'TDS not applicable', '/TDS:Exempt', 0, true)
    ON CONFLICT (company_id, name) DO NOTHING;

    -- Party Classification Tags
    INSERT INTO tags (company_id, name, tag_group, color, description, full_path, level, is_system)
    VALUES
        (p_company_id, 'Vendor:Supplier', 'party_type', '#3498DB', 'Regular goods supplier', '/Vendor:Supplier', 0, true),
        (p_company_id, 'Vendor:Contractor', 'party_type', '#E74C3C', 'Works contractor', '/Vendor:Contractor', 0, true),
        (p_company_id, 'Vendor:Consultant', 'party_type', '#9B59B6', 'Professional consultant', '/Vendor:Consultant', 0, true),
        (p_company_id, 'Vendor:Landlord', 'party_type', '#F39C12', 'Property lessor', '/Vendor:Landlord', 0, true),
        (p_company_id, 'Vendor:Service', 'party_type', '#1ABC9C', 'Service provider', '/Vendor:Service', 0, true),
        (p_company_id, 'Vendor:Foreign', 'party_type', '#34495E', 'Non-resident vendor', '/Vendor:Foreign', 0, true),
        (p_company_id, 'Customer:B2B', 'party_type', '#2ECC71', 'Business customer', '/Customer:B2B', 0, true),
        (p_company_id, 'Customer:B2C', 'party_type', '#27AE60', 'Consumer customer', '/Customer:B2C', 0, true),
        (p_company_id, 'Customer:Export', 'party_type', '#16A085', 'Export customer', '/Customer:Export', 0, true),
        (p_company_id, 'Customer:Government', 'party_type', '#145A32', 'Government entity', '/Customer:Government', 0, true),
        (p_company_id, 'MSME:Micro', 'compliance', '#FF9F43', 'MSME Micro enterprise', '/MSME:Micro', 0, true),
        (p_company_id, 'MSME:Small', 'compliance', '#FECA57', 'MSME Small enterprise', '/MSME:Small', 0, true),
        (p_company_id, 'MSME:Medium', 'compliance', '#EE5A24', 'MSME Medium enterprise', '/MSME:Medium', 0, true)
    ON CONFLICT (company_id, name) DO NOTHING;

    -- Seed TDS rules linked to tags
    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194C-Contractor')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   tds_rate_individual, tds_rate_company, threshold_annual, exemption_notes)
        VALUES (p_company_id, v_tag_id, '194C', 1.00, 20.00, 1.00, 2.00, 100000.00,
                'Exempt: Transporters owning ≤10 goods carriages with PAN')
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194J-Professional')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   threshold_annual, exemption_notes)
        VALUES (p_company_id, v_tag_id, '194J', 10.00, 20.00, 50000.00,
                'Legal, medical, engineering, architectural, accountancy, technical consultancy')
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194J-Technical')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_section_clause, tds_rate_with_pan,
                                   tds_rate_without_pan, threshold_annual, exemption_notes)
        VALUES (p_company_id, v_tag_id, '194J', '194J(ba)', 2.00, 20.00, 50000.00,
                'Technical services, royalty for cinematographic films')
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194H-Commission')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   threshold_annual)
        VALUES (p_company_id, v_tag_id, '194H', 5.00, 20.00, 15000.00)
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194I-Rent-Land')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_section_clause, tds_rate_with_pan,
                                   tds_rate_without_pan, threshold_annual)
        VALUES (p_company_id, v_tag_id, '194I', '194I(a)', 10.00, 20.00, 240000.00)
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194I-Rent-Equipment')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_section_clause, tds_rate_with_pan,
                                   tds_rate_without_pan, threshold_annual)
        VALUES (p_company_id, v_tag_id, '194I', '194I(b)', 2.00, 20.00, 240000.00)
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194A-Interest')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   threshold_annual, exemption_notes)
        VALUES (p_company_id, v_tag_id, '194A', 10.00, 20.00, 50000.00,
                'Interest other than securities. Senior citizens: Higher threshold')
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194Q-Purchase')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   threshold_annual, exemption_notes)
        VALUES (p_company_id, v_tag_id, '194Q', 0.10, 5.00, 5000000.00,
                'Only for buyers with turnover > ₹10 Cr. Not applicable if seller liable for TCS u/s 206C(1H)')
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:195-Foreign')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   threshold_annual, exemption_notes)
        VALUES (p_company_id, v_tag_id, '195', 10.00, 20.00, 0.00,
                'Rate varies by DTAA. Default 10% for royalties/FTS. Get CA certificate for DTAA rates')
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:194M-Individual')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   threshold_annual, exemption_notes)
        VALUES (p_company_id, v_tag_id, '194M', 5.00, 20.00, 5000000.00,
                'For individuals/HUF not liable for audit. Threshold: ₹50 lakh aggregate in FY')
        ON CONFLICT DO NOTHING;
    END LOOP;

    FOR v_tag_id IN (SELECT id FROM tags WHERE company_id = p_company_id AND name = 'TDS:Exempt')
    LOOP
        INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                                   threshold_annual, applies_to_government, exemption_notes)
        VALUES (p_company_id, v_tag_id, 'EXEMPT', 0.00, 0.00, 0.00, true,
                'Government entities, specified exempted parties')
        ON CONFLICT DO NOTHING;
    END LOOP;

END;
$$ LANGUAGE plpgsql;

-- 3. Add is_system column to tags if not exists (for system-seeded tags)
ALTER TABLE tags ADD COLUMN IF NOT EXISTS is_system BOOLEAN DEFAULT false;

-- 4. Trigger to auto-update updated_at
CREATE OR REPLACE FUNCTION update_tds_tag_rules_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_tds_tag_rules_timestamp ON tds_tag_rules;
CREATE TRIGGER trg_tds_tag_rules_timestamp
    BEFORE UPDATE ON tds_tag_rules
    FOR EACH ROW
    EXECUTE FUNCTION update_tds_tag_rules_timestamp();

COMMIT;
```

---

## Phase 7: Implementation Checklist

### Backend Tasks

| # | Task | File(s) | Priority |
|---|------|---------|----------|
| 1 | Create `tds_tag_rules` migration | `backend/migrations/140_*.sql` | P0 |
| 2 | Add `TdsTagRule` entity | `backend/src/Core/Entities/TdsTagRule.cs` | P0 |
| 3 | Create `ITdsTagRuleRepository` | `backend/src/Core/Interfaces/ITdsTagRuleRepository.cs` | P0 |
| 4 | Implement `TdsTagRuleRepository` | `backend/src/Infrastructure/Data/TdsTagRuleRepository.cs` | P0 |
| 5 | Create `TdsDetectionService` | `backend/src/Application/Services/TdsDetectionService.cs` | P0 |
| 6 | Update `TallyMasterMappingService` for auto-tagging | `backend/src/Application/Services/Migration/TallyMasterMappingService.cs` | P0 |
| 7 | Add `SeedTdsTagsAsync` to company setup | `backend/src/Application/Services/CompanyService.cs` | P1 |
| 8 | Update `VendorInvoicesService` to use new TDS detection | `backend/src/Application/Services/VendorInvoicesService.cs` | P1 |
| 9 | Update `VendorPaymentsService` for threshold tracking | `backend/src/Application/Services/VendorPaymentsService.cs` | P1 |
| 10 | Add API endpoints for TDS tag rules | `backend/src/WebApi/Controllers/TdsController.cs` | P1 |

### Frontend Tasks

| # | Task | File(s) | Priority |
|---|------|---------|----------|
| 1 | Complete `TallyMappingConfig` component | `apps/admin-portal/src/components/migration/TallyMappingConfig.tsx` | P0 |
| 2 | Add TDS tag picker in vendor forms | `apps/admin-portal/src/components/forms/VendorForm.tsx` | P1 |
| 3 | Create TDS rules management page | `apps/admin-portal/src/pages/settings/TdsRules.tsx` | P1 |
| 4 | Add TDS threshold warnings in payment forms | `apps/admin-portal/src/components/forms/VendorPaymentForm.tsx` | P1 |
| 5 | Show auto-detected tags during import preview | `apps/admin-portal/src/components/migration/TallyMasterPreview.tsx` | P2 |

---

## Indian Tax Law Compliance Notes

### TDS Sections Covered (Income Tax Act, 1961)

| Section | Payment Type | Rate (with PAN) | Rate (w/o PAN) | Threshold |
|---------|-------------|-----------------|----------------|-----------|
| 194A | Interest | 10% | 20% | ₹50,000/yr |
| 194C | Contractor | 1% (ind) / 2% (co) | 20% | ₹30,000 single / ₹1,00,000/yr |
| 194H | Commission | 5% | 20% | ₹15,000/yr |
| 194I(a) | Rent (land/building) | 10% | 20% | ₹2,40,000/yr |
| 194I(b) | Rent (P&M) | 2% | 20% | ₹2,40,000/yr |
| 194J | Professional fees | 10% | 20% | ₹50,000/yr |
| 194J(ba) | Technical services | 2% | 20% | ₹50,000/yr |
| 194M | Contractual to ind/HUF | 5% | 20% | ₹50,00,000/yr |
| 194Q | Purchase of goods | 0.1% | 5% | ₹50,00,000/yr |
| 195 | Foreign payments | DTAA rate / 10% | 20% | No threshold |

### Key Compliance Features

1. **PAN Verification**: Higher rate (20%) if PAN invalid/missing
2. **Lower TDS Certificate**: Support for Form 13 certificates
3. **Threshold Tracking**: YTD payment tracking per party
4. **Entity Type Detection**: Different rates for individuals vs companies (194C)
5. **DTAA Support**: Foreign payments at treaty rates (195)
6. **Government Exemption**: Auto-exempt government entities

---

## Summary

This plan transforms your system from a rigid, hard-coded TDS approach to a modern, tag-driven architecture that:

1. **Auto-classifies parties** during Tally import based on ledger groups
2. **Uses tags as the source of truth** for TDS behavior
3. **Learns from patterns** via attribution rules
4. **Stays compliant** with Indian tax law rates and thresholds
5. **Requires zero legacy migration** since you're starting fresh

The key insight is that your existing tags infrastructure is powerful but underutilized. By making tags drive TDS behavior (instead of hard-coded vendor types), you get a flexible, auditable, and user-configurable system that can adapt to changing tax laws by simply updating tag rules.
