-- Migration: Add 'resigned' status to employees table
-- This enables proper employee offboarding workflow

-- Update status constraint to include 'resigned'
ALTER TABLE employees
DROP CONSTRAINT IF EXISTS employees_status_check;

ALTER TABLE employees
ADD CONSTRAINT employees_status_check
CHECK (status IN ('active', 'inactive', 'terminated', 'resigned', 'permanent'));

-- Add resignation tracking fields to employees table for audit
ALTER TABLE employees
ADD COLUMN IF NOT EXISTS resigned_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS resignation_reason TEXT;
