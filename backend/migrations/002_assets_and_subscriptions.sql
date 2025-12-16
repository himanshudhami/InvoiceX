-- 002_assets_and_subscriptions.sql
-- Adds asset management and subscription tracking schema

-- Enum-like checks are enforced via CHECK constraints

-- Categories for grouping assets
CREATE TABLE IF NOT EXISTS asset_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    name VARCHAR(150) NOT NULL,
    code VARCHAR(50),
    asset_type VARCHAR(30) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT asset_categories_type_check CHECK (asset_type IN ('IT_Asset','Fixed_Asset','Intangible_Asset'))
);

-- Asset models to normalize make/model metadata
CREATE TABLE IF NOT EXISTS asset_models (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    category_id UUID REFERENCES asset_categories(id) ON DELETE SET NULL,
    manufacturer VARCHAR(150),
    model_name VARCHAR(150),
    model_number VARCHAR(100),
    specs JSONB,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Core assets table
CREATE TABLE IF NOT EXISTS assets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    category_id UUID REFERENCES asset_categories(id) ON DELETE SET NULL,
    model_id UUID REFERENCES asset_models(id) ON DELETE SET NULL,
    asset_type VARCHAR(30) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'available',
    asset_tag VARCHAR(100) NOT NULL,
    serial_number VARCHAR(150),
    name VARCHAR(200) NOT NULL,
    description TEXT,
    location VARCHAR(200),
    vendor VARCHAR(200),
    purchase_date DATE,
    in_service_date DATE,
    warranty_expiration DATE,
    purchase_cost NUMERIC(14,2),
    currency VARCHAR(10) DEFAULT 'USD',
    depreciation_method VARCHAR(30) DEFAULT 'none',
    useful_life_months INTEGER,
    salvage_value NUMERIC(14,2),
    residual_book_value NUMERIC(14,2),
    custom_properties JSONB,
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT assets_type_check CHECK (asset_type IN ('IT_Asset','Fixed_Asset','Intangible_Asset')),
    CONSTRAINT assets_status_check CHECK (status IN ('available','assigned','maintenance','retired','reserved','lost')),
    CONSTRAINT assets_depr_method_check CHECK (depreciation_method IN ('none','straight_line','double_declining','sum_of_years_digits'))
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_assets_company_tag ON assets(company_id, asset_tag);
CREATE INDEX IF NOT EXISTS idx_assets_company_type_status ON assets(company_id, asset_type, status);

-- Asset assignment to employee or company
CREATE TABLE IF NOT EXISTS asset_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES assets(id) ON DELETE CASCADE,
    target_type VARCHAR(20) NOT NULL,
    employee_id UUID REFERENCES employees(id) ON DELETE SET NULL,
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    assigned_on DATE NOT NULL DEFAULT CURRENT_DATE,
    returned_on DATE,
    condition_out TEXT,
    condition_in TEXT,
    is_active BOOLEAN GENERATED ALWAYS AS (returned_on IS NULL) STORED,
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT asset_assign_target_check CHECK (target_type IN ('employee','company')),
    CONSTRAINT asset_assign_target_fk CHECK (
        (target_type = 'employee' AND employee_id IS NOT NULL) OR
        (target_type = 'company' AND employee_id IS NULL)
    )
);
CREATE INDEX IF NOT EXISTS idx_asset_assign_active ON asset_assignments(asset_id) WHERE returned_on IS NULL;
CREATE INDEX IF NOT EXISTS idx_asset_assign_employee ON asset_assignments(employee_id) WHERE employee_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_asset_assign_company ON asset_assignments(company_id);

-- Maintenance / issue logs
CREATE TABLE IF NOT EXISTS asset_maintenance (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES assets(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    status VARCHAR(30) DEFAULT 'open',
    opened_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    closed_at TIMESTAMP WITHOUT TIME ZONE,
    vendor VARCHAR(200),
    cost NUMERIC(14,2),
    due_date DATE,
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT asset_maintenance_status_check CHECK (status IN ('open','in_progress','resolved','closed'))
);
CREATE INDEX IF NOT EXISTS idx_asset_maintenance_asset ON asset_maintenance(asset_id);

-- Asset documents (links to files/URLs)
CREATE TABLE IF NOT EXISTS asset_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES assets(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    url TEXT NOT NULL,
    content_type VARCHAR(100),
    uploaded_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    notes TEXT
);
CREATE INDEX IF NOT EXISTS idx_asset_documents_asset ON asset_documents(asset_id);

-- Asset audit/events
CREATE TABLE IF NOT EXISTS asset_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES assets(id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL,
    payload JSONB,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200)
);
CREATE INDEX IF NOT EXISTS idx_asset_events_asset ON asset_events(asset_id);

-- Depreciation snapshots (optional, for future accounting)
CREATE TABLE IF NOT EXISTS asset_depreciation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES assets(id) ON DELETE CASCADE,
    method VARCHAR(30) NOT NULL,
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    depreciation_amount NUMERIC(14,2) NOT NULL,
    accumulated_depreciation NUMERIC(14,2) NOT NULL,
    book_value NUMERIC(14,2) NOT NULL,
    run_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    notes TEXT,
    CONSTRAINT asset_depr_method_check CHECK (method IN ('straight_line','double_declining','sum_of_years_digits'))
);
CREATE INDEX IF NOT EXISTS idx_asset_depr_asset ON asset_depreciation(asset_id);

-- Subscriptions (software/services)
CREATE TABLE IF NOT EXISTS subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    vendor VARCHAR(200),
    plan_name VARCHAR(150),
    category VARCHAR(100),
    status VARCHAR(30) DEFAULT 'active',
    start_date DATE,
    renewal_date DATE,
    renewal_period VARCHAR(20) DEFAULT 'monthly',
    seats_total INTEGER,
    seats_used INTEGER,
    license_key TEXT,
    cost_per_period NUMERIC(14,2),
    currency VARCHAR(10) DEFAULT 'USD',
    billing_cycle_start DATE,
    billing_cycle_end DATE,
    auto_renew BOOLEAN DEFAULT TRUE,
    url TEXT,
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT subscriptions_status_check CHECK (status IN ('trial','active','on_hold','expired','cancelled')),
    CONSTRAINT subscriptions_period_check CHECK (renewal_period IN ('monthly','quarterly','yearly','custom'))
);
CREATE INDEX IF NOT EXISTS idx_subscriptions_company_status ON subscriptions(company_id, status);
CREATE INDEX IF NOT EXISTS idx_subscriptions_renewal ON subscriptions(renewal_date);

-- Subscription seat assignments
CREATE TABLE IF NOT EXISTS subscription_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID REFERENCES subscriptions(id) ON DELETE CASCADE,
    target_type VARCHAR(20) NOT NULL,
    employee_id UUID REFERENCES employees(id) ON DELETE SET NULL,
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    seat_identifier VARCHAR(200),
    role VARCHAR(100),
    assigned_on DATE NOT NULL DEFAULT CURRENT_DATE,
    revoked_on DATE,
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT subscription_assign_target_check CHECK (target_type IN ('employee','company')),
    CONSTRAINT subscription_assign_target_fk CHECK (
        (target_type = 'employee' AND employee_id IS NOT NULL) OR
        (target_type = 'company' AND employee_id IS NULL)
    )
);
CREATE INDEX IF NOT EXISTS idx_subscription_assign_active ON subscription_assignments(subscription_id) WHERE revoked_on IS NULL;
CREATE INDEX IF NOT EXISTS idx_subscription_assign_employee ON subscription_assignments(employee_id) WHERE employee_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_subscription_assign_company ON subscription_assignments(company_id);

-- Subscription events
CREATE TABLE IF NOT EXISTS subscription_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID REFERENCES subscriptions(id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL,
    payload JSONB,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200)
);
CREATE INDEX IF NOT EXISTS idx_subscription_events_sub ON subscription_events(subscription_id);

-- Seed minimal categories
INSERT INTO asset_categories (company_id, name, code, asset_type)
SELECT c.id, 'IT Assets', 'IT', 'IT_Asset' FROM companies c
ON CONFLICT DO NOTHING;

INSERT INTO asset_categories (company_id, name, code, asset_type)
SELECT c.id, 'Fixed Assets', 'FIX', 'Fixed_Asset' FROM companies c
ON CONFLICT DO NOTHING;

INSERT INTO asset_categories (company_id, name, code, asset_type)
SELECT c.id, 'Intangible Assets', 'INT', 'Intangible_Asset' FROM companies c
ON CONFLICT DO NOTHING;







