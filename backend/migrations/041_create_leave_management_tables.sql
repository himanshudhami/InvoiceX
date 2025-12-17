-- Migration: 041_create_leave_management_tables
-- Description: Creates leave management tables (leave_types, employee_leave_balances, leave_applications, holidays)
-- Date: 2025-12-17

-- ==================== Leave Types ====================
-- Defines leave types available in a company (CL, SL, EL, etc.)
CREATE TABLE IF NOT EXISTS leave_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(20) NOT NULL,
    description TEXT,
    days_per_year DECIMAL(5,2) NOT NULL,
    carry_forward_allowed BOOLEAN NOT NULL DEFAULT FALSE,
    max_carry_forward_days DECIMAL(5,2) NOT NULL DEFAULT 0,
    encashment_allowed BOOLEAN NOT NULL DEFAULT FALSE,
    max_encashment_days DECIMAL(5,2) NOT NULL DEFAULT 0,
    requires_approval BOOLEAN NOT NULL DEFAULT TRUE,
    min_days_notice INTEGER NOT NULL DEFAULT 0,
    max_consecutive_days INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    color_code VARCHAR(7) DEFAULT '#3B82F6',
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255),
    updated_by VARCHAR(255)
);

-- Unique constraint on company + code
CREATE UNIQUE INDEX idx_leave_types_company_code ON leave_types (company_id, code);

-- Index for active leave types
CREATE INDEX idx_leave_types_company_active ON leave_types (company_id, is_active) WHERE is_active = TRUE;

COMMENT ON TABLE leave_types IS 'Leave types configuration per company';
COMMENT ON COLUMN leave_types.code IS 'Short code like CL, SL, EL, PL, ML, etc.';
COMMENT ON COLUMN leave_types.days_per_year IS 'Annual leave quota for this type';
COMMENT ON COLUMN leave_types.carry_forward_allowed IS 'Whether unused leaves can carry forward';
COMMENT ON COLUMN leave_types.max_carry_forward_days IS 'Maximum days that can be carried forward';
COMMENT ON COLUMN leave_types.encashment_allowed IS 'Whether unused leaves can be encashed';
COMMENT ON COLUMN leave_types.min_days_notice IS 'Minimum days notice required for applying';
COMMENT ON COLUMN leave_types.color_code IS 'Hex color for UI display';

-- ==================== Employee Leave Balances ====================
-- Tracks leave balance per employee per leave type per financial year
CREATE TABLE IF NOT EXISTS employee_leave_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    leave_type_id UUID NOT NULL REFERENCES leave_types(id) ON DELETE CASCADE,
    financial_year VARCHAR(10) NOT NULL,
    opening_balance DECIMAL(5,2) NOT NULL DEFAULT 0,
    accrued DECIMAL(5,2) NOT NULL DEFAULT 0,
    taken DECIMAL(5,2) NOT NULL DEFAULT 0,
    carry_forwarded DECIMAL(5,2) NOT NULL DEFAULT 0,
    adjusted DECIMAL(5,2) NOT NULL DEFAULT 0,
    encashed DECIMAL(5,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_leave_balance_non_negative CHECK (
        opening_balance >= 0 AND
        accrued >= 0 AND
        taken >= 0 AND
        carry_forwarded >= 0 AND
        encashed >= 0
    )
);

-- Unique constraint on employee + leave type + year
CREATE UNIQUE INDEX idx_leave_balances_employee_type_year
    ON employee_leave_balances (employee_id, leave_type_id, financial_year);

-- Index for employee lookup
CREATE INDEX idx_leave_balances_employee ON employee_leave_balances (employee_id);

-- Index for financial year lookup
CREATE INDEX idx_leave_balances_year ON employee_leave_balances (financial_year);

COMMENT ON TABLE employee_leave_balances IS 'Leave balance tracking per employee per year';
COMMENT ON COLUMN employee_leave_balances.financial_year IS 'Financial year in format YYYY-YY (e.g., 2024-25)';
COMMENT ON COLUMN employee_leave_balances.opening_balance IS 'Balance at start of year';
COMMENT ON COLUMN employee_leave_balances.accrued IS 'Leaves accrued during the year (for monthly accrual)';
COMMENT ON COLUMN employee_leave_balances.taken IS 'Leaves taken/approved';
COMMENT ON COLUMN employee_leave_balances.carry_forwarded IS 'Leaves carried forward from previous year';
COMMENT ON COLUMN employee_leave_balances.adjusted IS 'Manual adjustments (+/-)';
COMMENT ON COLUMN employee_leave_balances.encashed IS 'Leaves encashed';

-- ==================== Leave Applications ====================
-- Leave applications submitted by employees
CREATE TABLE IF NOT EXISTS leave_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    leave_type_id UUID NOT NULL REFERENCES leave_types(id) ON DELETE RESTRICT,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    from_date DATE NOT NULL,
    to_date DATE NOT NULL,
    total_days DECIMAL(5,2) NOT NULL,
    is_half_day BOOLEAN NOT NULL DEFAULT FALSE,
    half_day_type VARCHAR(20),
    reason TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    applied_at TIMESTAMP NOT NULL DEFAULT NOW(),
    approved_by UUID REFERENCES employees(id) ON DELETE SET NULL,
    approved_at TIMESTAMP,
    rejection_reason TEXT,
    cancelled_at TIMESTAMP,
    cancellation_reason TEXT,
    emergency_contact VARCHAR(100),
    handover_notes TEXT,
    attachment_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_leave_dates CHECK (to_date >= from_date),
    CONSTRAINT chk_leave_total_days CHECK (total_days > 0),
    CONSTRAINT chk_leave_status CHECK (
        status IN ('pending', 'approved', 'rejected', 'cancelled', 'withdrawn')
    ),
    CONSTRAINT chk_half_day_type CHECK (
        (is_half_day = FALSE AND half_day_type IS NULL) OR
        (is_half_day = TRUE AND half_day_type IN ('first_half', 'second_half'))
    )
);

-- Index for employee lookup
CREATE INDEX idx_leave_applications_employee ON leave_applications (employee_id);

-- Index for status lookup
CREATE INDEX idx_leave_applications_status ON leave_applications (status);

-- Index for date range queries
CREATE INDEX idx_leave_applications_dates ON leave_applications (from_date, to_date);

-- Index for pending approvals
CREATE INDEX idx_leave_applications_pending ON leave_applications (company_id, status)
    WHERE status = 'pending';

-- Index for company and date range (for reports)
CREATE INDEX idx_leave_applications_company_dates ON leave_applications (company_id, from_date, to_date);

COMMENT ON TABLE leave_applications IS 'Employee leave applications';
COMMENT ON COLUMN leave_applications.total_days IS 'Total leave days (considering holidays, half-days)';
COMMENT ON COLUMN leave_applications.is_half_day IS 'Whether this is a half-day leave';
COMMENT ON COLUMN leave_applications.half_day_type IS 'first_half or second_half if half-day';
COMMENT ON COLUMN leave_applications.status IS 'pending, approved, rejected, cancelled, withdrawn';
COMMENT ON COLUMN leave_applications.emergency_contact IS 'Emergency contact during leave';
COMMENT ON COLUMN leave_applications.handover_notes IS 'Work handover notes';

-- ==================== Holidays ====================
-- Company holiday calendar
CREATE TABLE IF NOT EXISTS holidays (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    date DATE NOT NULL,
    year INTEGER NOT NULL,
    is_optional BOOLEAN NOT NULL DEFAULT FALSE,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Unique constraint on company + date
CREATE UNIQUE INDEX idx_holidays_company_date ON holidays (company_id, date);

-- Index for year lookup
CREATE INDEX idx_holidays_year ON holidays (company_id, year);

-- Index for date range queries
CREATE INDEX idx_holidays_date ON holidays (date);

COMMENT ON TABLE holidays IS 'Company holiday calendar';
COMMENT ON COLUMN holidays.is_optional IS 'Whether employees can work and get substitute leave';
COMMENT ON COLUMN holidays.year IS 'Calendar year for easy filtering';

-- ==================== Insert Default Leave Types ====================
-- This is a template - actual data will be inserted when a company is created
-- or via admin UI. Uncomment and modify company_id to insert defaults.

/*
-- Example default leave types for a company (replace company_id)
INSERT INTO leave_types (company_id, name, code, days_per_year, carry_forward_allowed, max_carry_forward_days, sort_order) VALUES
    ('COMPANY_UUID_HERE', 'Casual Leave', 'CL', 12, false, 0, 1),
    ('COMPANY_UUID_HERE', 'Sick Leave', 'SL', 12, false, 0, 2),
    ('COMPANY_UUID_HERE', 'Earned Leave', 'EL', 15, true, 30, 3),
    ('COMPANY_UUID_HERE', 'Compensatory Off', 'COMP', 0, false, 0, 4),
    ('COMPANY_UUID_HERE', 'Work From Home', 'WFH', 0, false, 0, 5);
*/
