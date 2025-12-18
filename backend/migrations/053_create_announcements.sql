-- Migration: 053_create_announcements
-- Description: Creates announcements table for company-wide notifications
-- Date: 2025-12-17

-- ==================== Announcements ====================
-- Company announcements for employees
CREATE TABLE IF NOT EXISTS announcements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    category VARCHAR(50) NOT NULL DEFAULT 'general',
    priority VARCHAR(20) NOT NULL DEFAULT 'normal',
    is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
    published_at TIMESTAMP,
    expires_at TIMESTAMP,
    created_by UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_announcement_category CHECK (
        category IN ('general', 'hr', 'policy', 'event', 'celebration')
    ),
    CONSTRAINT chk_announcement_priority CHECK (
        priority IN ('low', 'normal', 'high', 'urgent')
    )
);

-- Index for company lookup
CREATE INDEX idx_announcements_company ON announcements(company_id);

-- Index for published announcements
CREATE INDEX idx_announcements_published ON announcements(company_id, published_at)
    WHERE published_at IS NOT NULL;

-- Index for pinned announcements
CREATE INDEX idx_announcements_pinned ON announcements(company_id, is_pinned)
    WHERE is_pinned = TRUE;

COMMENT ON TABLE announcements IS 'Company-wide announcements for employees';
COMMENT ON COLUMN announcements.category IS 'Type: general, hr, policy, event, celebration';
COMMENT ON COLUMN announcements.priority IS 'Display priority: low, normal, high, urgent';
COMMENT ON COLUMN announcements.is_pinned IS 'Pinned announcements appear at the top';
COMMENT ON COLUMN announcements.published_at IS 'NULL means draft, set to publish';
COMMENT ON COLUMN announcements.expires_at IS 'Auto-hide after this date';

-- ==================== Announcement Reads ====================
-- Track which employees have read which announcements
CREATE TABLE IF NOT EXISTS announcement_reads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    announcement_id UUID NOT NULL REFERENCES announcements(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    read_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_announcement_employee UNIQUE(announcement_id, employee_id)
);

-- Index for employee's read announcements
CREATE INDEX idx_announcement_reads_employee ON announcement_reads(employee_id);

-- Index for announcement read count
CREATE INDEX idx_announcement_reads_announcement ON announcement_reads(announcement_id);

COMMENT ON TABLE announcement_reads IS 'Tracks employee read status for announcements';
