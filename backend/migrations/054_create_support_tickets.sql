-- Migration: 054_create_support_tickets
-- Description: Creates support ticket system for employee help desk
-- Date: 2025-12-17

-- ==================== Support Tickets ====================
-- Employee support/help desk tickets
CREATE TABLE IF NOT EXISTS support_tickets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    ticket_number VARCHAR(20) NOT NULL UNIQUE,
    subject VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    category VARCHAR(50) NOT NULL,
    priority VARCHAR(20) NOT NULL DEFAULT 'medium',
    status VARCHAR(30) NOT NULL DEFAULT 'open',
    assigned_to UUID REFERENCES users(id) ON DELETE SET NULL,
    resolved_at TIMESTAMP,
    resolution_notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_ticket_category CHECK (
        category IN ('payroll', 'leave', 'it', 'hr', 'assets', 'general')
    ),
    CONSTRAINT chk_ticket_priority CHECK (
        priority IN ('low', 'medium', 'high', 'urgent')
    ),
    CONSTRAINT chk_ticket_status CHECK (
        status IN ('open', 'in_progress', 'waiting_on_employee', 'resolved', 'closed')
    )
);

-- Index for employee's tickets
CREATE INDEX idx_tickets_employee ON support_tickets(employee_id);

-- Index for company tickets by status
CREATE INDEX idx_tickets_company_status ON support_tickets(company_id, status);

-- Index for assigned tickets
CREATE INDEX idx_tickets_assigned ON support_tickets(assigned_to)
    WHERE assigned_to IS NOT NULL;

-- Index for open/pending tickets
CREATE INDEX idx_tickets_open ON support_tickets(company_id, created_at)
    WHERE status IN ('open', 'in_progress', 'waiting_on_employee');

COMMENT ON TABLE support_tickets IS 'Employee support/help desk tickets';
COMMENT ON COLUMN support_tickets.ticket_number IS 'Human-readable ticket ID (e.g., TKT-2024-0001)';
COMMENT ON COLUMN support_tickets.category IS 'Department category: payroll, leave, it, hr, assets, general';
COMMENT ON COLUMN support_tickets.status IS 'Ticket status: open, in_progress, waiting_on_employee, resolved, closed';

-- ==================== Support Ticket Messages ====================
-- Conversation thread for support tickets
CREATE TABLE IF NOT EXISTS support_ticket_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
    sender_id UUID NOT NULL,
    sender_type VARCHAR(20) NOT NULL,
    message TEXT NOT NULL,
    attachment_url TEXT,
    attachment_name VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_sender_type CHECK (
        sender_type IN ('employee', 'admin', 'system')
    )
);

-- Index for ticket messages
CREATE INDEX idx_ticket_messages_ticket ON support_ticket_messages(ticket_id, created_at);

COMMENT ON TABLE support_ticket_messages IS 'Conversation thread for support tickets';
COMMENT ON COLUMN support_ticket_messages.sender_type IS 'Who sent the message: employee, admin, or system';

-- ==================== FAQ Items ====================
-- Frequently asked questions for self-service
CREATE TABLE IF NOT EXISTS faq_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    category VARCHAR(50) NOT NULL,
    question TEXT NOT NULL,
    answer TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    view_count INTEGER NOT NULL DEFAULT 0,
    helpful_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Index for active FAQs by category
CREATE INDEX idx_faq_active_category ON faq_items(category, sort_order)
    WHERE is_active = TRUE;

-- Index for company-specific FAQs
CREATE INDEX idx_faq_company ON faq_items(company_id)
    WHERE company_id IS NOT NULL;

COMMENT ON TABLE faq_items IS 'Frequently asked questions for employee self-service';
COMMENT ON COLUMN faq_items.company_id IS 'NULL means global FAQ, not company-specific';
COMMENT ON COLUMN faq_items.view_count IS 'Number of times this FAQ was viewed';
COMMENT ON COLUMN faq_items.helpful_count IS 'Number of times marked as helpful';

-- ==================== Create sequence for ticket numbers ====================
CREATE SEQUENCE IF NOT EXISTS ticket_number_seq START 1;

-- ==================== Function to generate ticket number ====================
CREATE OR REPLACE FUNCTION generate_ticket_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.ticket_number := 'TKT-' || EXTRACT(YEAR FROM NOW()) || '-' || LPAD(nextval('ticket_number_seq')::TEXT, 5, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ==================== Trigger to auto-generate ticket number ====================
DROP TRIGGER IF EXISTS trg_generate_ticket_number ON support_tickets;
CREATE TRIGGER trg_generate_ticket_number
    BEFORE INSERT ON support_tickets
    FOR EACH ROW
    WHEN (NEW.ticket_number IS NULL OR NEW.ticket_number = '')
    EXECUTE FUNCTION generate_ticket_number();
