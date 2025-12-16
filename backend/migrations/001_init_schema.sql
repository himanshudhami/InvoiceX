-- 001_init_schema.sql
-- Establishes the current InvoiceApp schema (idempotent)

-- Extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Sequences
CREATE SEQUENCE IF NOT EXISTS schemaversions_schemaversionsid_seq START 1;
CREATE SEQUENCE IF NOT EXISTS test_table_id_seq START 1;

-- Companies
CREATE TABLE IF NOT EXISTS companies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    logo_url TEXT,
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    zip_code VARCHAR(20),
    country VARCHAR(100),
    email VARCHAR(255),
    phone VARCHAR(50),
    website VARCHAR(255),
    tax_number VARCHAR(100),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    payment_instructions TEXT,
    signature_type VARCHAR(50),
    signature_data TEXT,
    signature_name VARCHAR(255),
    signature_font VARCHAR(100),
    signature_color VARCHAR(50),
    sownumberprefix VARCHAR(10),
    sownumbercounter INTEGER DEFAULT 1,
    sowdefaultterms TEXT,
    sowdefaultpaymentterms TEXT,
    sowdefaultchangemanagementprocess TEXT,
    sowtemplate TEXT,
    invoice_template_id UUID
);

-- Customers
CREATE TABLE IF NOT EXISTS customers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    name VARCHAR(255) NOT NULL,
    company_name VARCHAR(255),
    email VARCHAR(255),
    phone VARCHAR(50),
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    zip_code VARCHAR(20),
    country VARCHAR(100),
    tax_number VARCHAR(100),
    notes TEXT,
    credit_limit NUMERIC(12,2),
    payment_terms INTEGER DEFAULT 30,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Products
CREATE TABLE IF NOT EXISTS products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    sku VARCHAR(100),
    category VARCHAR(100),
    type VARCHAR(50),
    unit_price NUMERIC(12,2) NOT NULL,
    unit VARCHAR(50) DEFAULT 'unit',
    tax_rate NUMERIC(5,2) DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Invoice Templates
CREATE TABLE IF NOT EXISTS invoice_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    name VARCHAR(100) NOT NULL,
    template_data JSONB NOT NULL,
    is_default BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    template_key VARCHAR(100),
    preview_url TEXT,
    config_schema JSONB
);

-- Invoices
CREATE TABLE IF NOT EXISTS invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    customer_id UUID REFERENCES customers(id),
    invoice_number VARCHAR(50) NOT NULL,
    invoice_date DATE NOT NULL,
    due_date DATE NOT NULL,
    status VARCHAR(50) DEFAULT 'draft',
    subtotal NUMERIC(12,2) NOT NULL,
    tax_amount NUMERIC(12,2) DEFAULT 0,
    discount_amount NUMERIC(12,2) DEFAULT 0,
    total_amount NUMERIC(12,2) NOT NULL,
    paid_amount NUMERIC(12,2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'USD',
    notes TEXT,
    terms TEXT,
    po_number VARCHAR(100),
    project_name VARCHAR(255),
    sent_at TIMESTAMP WITHOUT TIME ZONE,
    viewed_at TIMESTAMP WITHOUT TIME ZONE,
    paid_at TIMESTAMP WITHOUT TIME ZONE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    payment_instructions TEXT,
    document_type VARCHAR(20) NOT NULL DEFAULT 'invoice',
    valid_until_date DATE,
    reference_number VARCHAR(100),
    approved_at TIMESTAMP WITHOUT TIME ZONE,
    template_id UUID,
    CONSTRAINT invoices_status_check CHECK (status IN ('draft','sent','viewed','partially_paid','paid','overdue','cancelled')),
    CONSTRAINT chk_document_type CHECK (document_type IN ('invoice','sow','quote','msa','contract'))
);

-- Invoice Items
CREATE TABLE IF NOT EXISTS invoice_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID REFERENCES invoices(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id),
    description TEXT NOT NULL,
    quantity NUMERIC(12,3) NOT NULL,
    unit_price NUMERIC(12,2) NOT NULL,
    tax_rate NUMERIC(5,2) DEFAULT 0,
    discount_rate NUMERIC(5,2) DEFAULT 0,
    line_total NUMERIC(12,2) NOT NULL,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Payments
CREATE TABLE IF NOT EXISTS payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID REFERENCES invoices(id),
    payment_date DATE NOT NULL,
    amount NUMERIC(12,2) NOT NULL,
    payment_method VARCHAR(50),
    reference_number VARCHAR(100),
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tax Rates
CREATE TABLE IF NOT EXISTS tax_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    name VARCHAR(100) NOT NULL,
    rate NUMERIC(5,2) NOT NULL,
    is_default BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Quotes
CREATE TABLE IF NOT EXISTS quotes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    customer_id UUID REFERENCES customers(id),
    quote_number VARCHAR(50) NOT NULL,
    quote_date DATE NOT NULL,
    expiry_date DATE NOT NULL,
    status VARCHAR(50) DEFAULT 'draft',
    subtotal NUMERIC(12,2) NOT NULL DEFAULT 0,
    discount_type VARCHAR(20) DEFAULT 'percentage',
    discount_value NUMERIC(12,2) DEFAULT 0,
    discount_amount NUMERIC(12,2) DEFAULT 0,
    tax_amount NUMERIC(12,2) DEFAULT 0,
    total_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'USD',
    notes TEXT,
    terms TEXT,
    payment_instructions TEXT,
    po_number VARCHAR(100),
    project_name VARCHAR(255),
    sent_at TIMESTAMP WITHOUT TIME ZONE,
    viewed_at TIMESTAMP WITHOUT TIME ZONE,
    accepted_at TIMESTAMP WITHOUT TIME ZONE,
    rejected_at TIMESTAMP WITHOUT TIME ZONE,
    rejected_reason TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT quotes_status_check CHECK (status IN ('draft','sent','viewed','accepted','rejected','expired','cancelled')),
    CONSTRAINT quotes_discount_type_check CHECK (discount_type IN ('percentage','fixed')),
    CONSTRAINT quotes_quote_number_key UNIQUE (quote_number)
);

-- Quote Items
CREATE TABLE IF NOT EXISTS quote_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quote_id UUID REFERENCES quotes(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id),
    description TEXT NOT NULL,
    quantity NUMERIC(12,3) NOT NULL,
    unit_price NUMERIC(12,2) NOT NULL,
    tax_rate NUMERIC(5,2) DEFAULT 0,
    discount_rate NUMERIC(5,2) DEFAULT 0,
    line_total NUMERIC(12,2) NOT NULL,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Employees
CREATE TABLE IF NOT EXISTS employees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_name VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    phone VARCHAR(50),
    employee_id VARCHAR(100),
    department VARCHAR(255),
    designation VARCHAR(255),
    hire_date DATE,
    status VARCHAR(50) DEFAULT 'active',
    bank_account_number VARCHAR(100),
    bank_name VARCHAR(255),
    ifsc_code VARCHAR(20),
    pan_number VARCHAR(20),
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    zip_code VARCHAR(20),
    country VARCHAR(100) DEFAULT 'India',
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    contract_type VARCHAR(100),
    company VARCHAR(255),
    CONSTRAINT employees_employee_id_key UNIQUE (employee_id),
    CONSTRAINT employees_status_check CHECK (status IN ('active','inactive','terminated','permanent'))
);

-- Employee Salary Transactions
CREATE TABLE IF NOT EXISTS employee_salary_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    salary_month INTEGER NOT NULL,
    salary_year INTEGER NOT NULL,
    basic_salary NUMERIC(10,2) NOT NULL DEFAULT 0,
    hra NUMERIC(10,2) NOT NULL DEFAULT 0,
    conveyance NUMERIC(10,2) NOT NULL DEFAULT 0,
    medical_allowance NUMERIC(10,2) NOT NULL DEFAULT 0,
    special_allowance NUMERIC(10,2) NOT NULL DEFAULT 0,
    lta NUMERIC(10,2) NOT NULL DEFAULT 0,
    other_allowances NUMERIC(10,2) NOT NULL DEFAULT 0,
    gross_salary NUMERIC(10,2) NOT NULL DEFAULT 0,
    pf_employee NUMERIC(10,2) NOT NULL DEFAULT 0,
    pf_employer NUMERIC(10,2) NOT NULL DEFAULT 0,
    pt NUMERIC(10,2) NOT NULL DEFAULT 0,
    income_tax NUMERIC(10,2) NOT NULL DEFAULT 0,
    other_deductions NUMERIC(10,2) NOT NULL DEFAULT 0,
    net_salary NUMERIC(10,2) NOT NULL DEFAULT 0,
    payment_date DATE,
    payment_method VARCHAR(50) DEFAULT 'bank_transfer',
    payment_reference VARCHAR(255),
    status VARCHAR(50) DEFAULT 'pending',
    remarks TEXT,
    currency VARCHAR(10) DEFAULT 'INR',
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    CONSTRAINT employee_salary_transactions_salary_month_check CHECK (salary_month >= 1 AND salary_month <= 12),
    CONSTRAINT employee_salary_transactions_salary_year_check CHECK (salary_year >= 2000 AND salary_year <= 2100),
    CONSTRAINT employee_salary_transactions_payment_method_check CHECK (payment_method IN ('bank_transfer','cash','check')),
    CONSTRAINT employee_salary_transactions_status_check CHECK (status IN ('pending','processed','paid','cancelled'))
);

-- Documents (no primary key defined in current schema)
CREATE TABLE IF NOT EXISTS documents (
    id UUID,
    document_type VARCHAR(20),
    document_number VARCHAR(50),
    company_id UUID,
    customer_id UUID,
    document_date DATE,
    valid_until_date DATE,
    status VARCHAR(50),
    subtotal NUMERIC(12,2),
    tax_amount NUMERIC(12,2),
    discount_amount NUMERIC(12,2),
    total_amount NUMERIC(12,2),
    paid_amount NUMERIC(12,2),
    currency VARCHAR(3),
    notes TEXT,
    terms TEXT,
    payment_instructions TEXT,
    reference_number VARCHAR(100),
    project_name VARCHAR(255),
    sent_at TIMESTAMP WITHOUT TIME ZONE,
    viewed_at TIMESTAMP WITHOUT TIME ZONE,
    approved_at TIMESTAMP WITHOUT TIME ZONE,
    paid_at TIMESTAMP WITHOUT TIME ZONE,
    created_at TIMESTAMP WITHOUT TIME ZONE,
    updated_at TIMESTAMP WITHOUT TIME ZONE,
    template_id UUID
);

-- MSAs
CREATE TABLE IF NOT EXISTS msas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE SET NULL,
    customer_id UUID REFERENCES customers(id) ON DELETE SET NULL,
    msa_number VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    effective_date DATE,
    expiration_date DATE,
    status VARCHAR(20) DEFAULT 'draft',
    total_value NUMERIC(15,2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'USD',
    scope_of_services TEXT,
    payment_terms TEXT,
    performance_standards TEXT,
    confidentiality_terms TEXT,
    intellectual_property_rights TEXT,
    warranties TEXT,
    liability_limitations TEXT,
    indemnification_terms TEXT,
    termination_conditions TEXT,
    dispute_resolution TEXT,
    governing_law VARCHAR(200),
    jurisdiction VARCHAR(200),
    project_name VARCHAR(200),
    po_number VARCHAR(100),
    notes TEXT,
    sent_at TIMESTAMPTZ,
    viewed_at TIMESTAMPTZ,
    accepted_at TIMESTAMPTZ,
    activated_at TIMESTAMPTZ,
    expired_at TIMESTAMPTZ,
    terminated_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT msas_msa_number_key UNIQUE (msa_number)
);

-- MSA Items
CREATE TABLE IF NOT EXISTS msa_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    msa_id UUID NOT NULL REFERENCES msas(id),
    product_id UUID REFERENCES products(id),
    description VARCHAR(500) NOT NULL,
    quantity NUMERIC(10,2) DEFAULT 1,
    unit_price NUMERIC(15,2) DEFAULT 0,
    tax_rate NUMERIC(5,2) DEFAULT 0,
    discount_rate NUMERIC(5,2) DEFAULT 0,
    line_total NUMERIC(15,2) DEFAULT 0,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- SOWs
CREATE TABLE IF NOT EXISTS sows (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    customer_id UUID REFERENCES customers(id),
    sow_number VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    start_date DATE,
    end_date DATE,
    status VARCHAR(20) DEFAULT 'draft',
    total_value NUMERIC(15,2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'USD',
    scope_of_work TEXT,
    deliverables TEXT,
    terms TEXT,
    payment_terms TEXT,
    project_name VARCHAR(200),
    po_number VARCHAR(100),
    notes TEXT,
    sent_at TIMESTAMPTZ,
    viewed_at TIMESTAMPTZ,
    accepted_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT sows_sow_number_key UNIQUE (sow_number)
);

-- SOW Items
CREATE TABLE IF NOT EXISTS sow_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sow_id UUID NOT NULL REFERENCES sows(id),
    description TEXT NOT NULL,
    quantity NUMERIC(10,2) DEFAULT 1,
    unit_price NUMERIC(15,2) DEFAULT 0,
    tax_rate NUMERIC(5,2) DEFAULT 0,
    discount_rate NUMERIC(5,2) DEFAULT 0,
    line_total NUMERIC(15,2) DEFAULT 0,
    delivery_date DATE,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Test Table
CREATE TABLE IF NOT EXISTS test_table (
    id INTEGER NOT NULL DEFAULT nextval('test_table_id_seq'),
    name VARCHAR(100),
    CONSTRAINT test_table_pkey PRIMARY KEY (id)
);
ALTER SEQUENCE IF EXISTS test_table_id_seq OWNED BY test_table.id;

-- DbUp journal
CREATE TABLE IF NOT EXISTS schemaversions (
    schemaversionsid INTEGER NOT NULL DEFAULT nextval('schemaversions_schemaversionsid_seq'),
    scriptname VARCHAR NOT NULL,
    applied TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    CONSTRAINT PK_schemaversions_Id PRIMARY KEY (schemaversionsid)
);
ALTER SEQUENCE IF EXISTS schemaversions_schemaversionsid_seq OWNED BY schemaversions.schemaversionsid;

-- Foreign key that references invoice_templates after both tables exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'companies_invoice_template_id_fkey'
    ) THEN
        ALTER TABLE companies
            ADD CONSTRAINT companies_invoice_template_id_fkey FOREIGN KEY (invoice_template_id) REFERENCES invoice_templates(id);
    END IF;
END $$;

-- Indexes
CREATE INDEX IF NOT EXISTS idx_customers_company_id ON customers(company_id);
CREATE INDEX IF NOT EXISTS idx_customers_email ON customers(email);

CREATE INDEX IF NOT EXISTS idx_products_company_id ON products(company_id);
CREATE INDEX IF NOT EXISTS idx_products_sku ON products(sku);

CREATE INDEX IF NOT EXISTS idx_invoices_company_id ON invoices(company_id);
CREATE INDEX IF NOT EXISTS idx_invoices_customer_id ON invoices(customer_id);
CREATE INDEX IF NOT EXISTS idx_invoices_invoice_number ON invoices(invoice_number);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON invoices(status);
CREATE INDEX IF NOT EXISTS idx_invoices_due_date ON invoices(due_date);
CREATE INDEX IF NOT EXISTS idx_invoices_document_type ON invoices(document_type);

CREATE INDEX IF NOT EXISTS idx_invoice_items_invoice_id ON invoice_items(invoice_id);
CREATE INDEX IF NOT EXISTS idx_invoice_items_product_id ON invoice_items(product_id);

CREATE INDEX IF NOT EXISTS idx_payments_invoice_id ON payments(invoice_id);
CREATE INDEX IF NOT EXISTS idx_payments_payment_date ON payments(payment_date);

CREATE INDEX IF NOT EXISTS idx_tax_rates_company_id ON tax_rates(company_id);

CREATE INDEX IF NOT EXISTS idx_quotes_company_id ON quotes(company_id);
CREATE INDEX IF NOT EXISTS idx_quotes_customer_id ON quotes(customer_id);
CREATE INDEX IF NOT EXISTS idx_quotes_quote_number ON quotes(quote_number);
CREATE INDEX IF NOT EXISTS idx_quotes_status ON quotes(status);
CREATE INDEX IF NOT EXISTS idx_quotes_expiry_date ON quotes(expiry_date);
CREATE INDEX IF NOT EXISTS idx_quotes_created_at ON quotes(created_at);

CREATE INDEX IF NOT EXISTS idx_quote_items_quote_id ON quote_items(quote_id);
CREATE INDEX IF NOT EXISTS idx_quote_items_product_id ON quote_items(product_id);

CREATE INDEX IF NOT EXISTS idx_employees_employee_id ON employees(employee_id);
CREATE INDEX IF NOT EXISTS idx_employees_status ON employees(status);
CREATE INDEX IF NOT EXISTS idx_employees_department ON employees(department);
CREATE INDEX IF NOT EXISTS idx_employees_email ON employees(email);

CREATE INDEX IF NOT EXISTS idx_employee_salary_unique ON employee_salary_transactions(employee_id, salary_month, salary_year);
CREATE INDEX IF NOT EXISTS idx_employee_salary_employee_id ON employee_salary_transactions(employee_id);
CREATE INDEX IF NOT EXISTS idx_employee_salary_month_year ON employee_salary_transactions(salary_year, salary_month);
CREATE INDEX IF NOT EXISTS idx_employee_salary_status ON employee_salary_transactions(status);
CREATE INDEX IF NOT EXISTS idx_employee_salary_payment_date ON employee_salary_transactions(payment_date);

CREATE INDEX IF NOT EXISTS idx_msas_company_id ON msas(company_id);
CREATE INDEX IF NOT EXISTS idx_msas_customer_id ON msas(customer_id);
CREATE INDEX IF NOT EXISTS idx_msas_status ON msas(status);
CREATE INDEX IF NOT EXISTS idx_msas_msa_number ON msas(msa_number);
CREATE INDEX IF NOT EXISTS idx_msas_effective_date ON msas(effective_date);
CREATE INDEX IF NOT EXISTS idx_msas_expiration_date ON msas(expiration_date);

CREATE INDEX IF NOT EXISTS idx_msa_items_msa_id ON msa_items(msa_id);
CREATE INDEX IF NOT EXISTS idx_msa_items_product_id ON msa_items(product_id);
CREATE INDEX IF NOT EXISTS idx_msa_items_sort_order ON msa_items(msa_id, sort_order);

CREATE INDEX IF NOT EXISTS idx_sows_company_id ON sows(company_id);
CREATE INDEX IF NOT EXISTS idx_sows_customer_id ON sows(customer_id);
CREATE INDEX IF NOT EXISTS idx_sows_status ON sows(status);
CREATE INDEX IF NOT EXISTS idx_sows_sow_number ON sows(sow_number);
CREATE INDEX IF NOT EXISTS idx_sows_start_date ON sows(start_date);
CREATE INDEX IF NOT EXISTS idx_sows_end_date ON sows(end_date);

CREATE INDEX IF NOT EXISTS idx_sow_items_sow_id ON sow_items(sow_id);
CREATE INDEX IF NOT EXISTS idx_sow_items_delivery_date ON sow_items(delivery_date);
CREATE INDEX IF NOT EXISTS idx_sow_items_sort_order ON sow_items(sow_id, sort_order);








